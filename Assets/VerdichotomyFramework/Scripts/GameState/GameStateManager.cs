using System;
using System.Collections.Generic;
using UnityEngine;
using VerdichotomyFramework.Cards.Data;
using VerdichotomyFramework.Cards.Flags;
namespace VerdichotomyFramework.GameState
{
    /// <summary>
    /// The single source of truth for all runtime state:
    /// stat values, run flags, campaign flags, turn counter, and visit counts.
    /// Persistence is handled here via a simple JSON save system.
    /// Other systems read from and write to this manager; they never
    /// cache state themselves.
    /// </summary>
    public class GameStateManager : MonoBehaviour
	{

		// ── Persistence ───────────────────────────────────────────────────────

		private const string CampaignSaveKey = "VerdichotomyFramework_Campaign";
		// ── Dependencies ──────────────────────────────────────────────────────

		[SerializeField, Tooltip("The game configuration asset.")]
		private GameConfig config;
		private readonly Dictionary<string, int> campaignFlags = new();

		private readonly Dictionary<string, int> lastSeenTurn = new(); // cardId → turn last shown
		private readonly Dictionary<string, int> runFlags = new();

		// ── State ─────────────────────────────────────────────────────────────

		private readonly Dictionary<string, int> stats = new();
		private readonly Dictionary<string, int> visitCounts = new(); // cardId → visit count

		public int CurrentTurn { get; private set; }
		
		// ── Events ────────────────────────────────────────────────────────────

		// TODO: Updates these comments properly
		
		/// <summary>
		/// statId, oldValue, newValue
		/// </summary>
		public event Action<string, int, int> OnStatChanged;
		
		/// <summary>
		/// statId
		/// </summary>
		public event Action<string> OnStatLethal; 
		
		/// <summary>
		/// flagId, newValue
		/// </summary>
		public event Action<string, int> OnFlagChanged; 
		public event Action OnRunStarted;
		public event Action OnRunEnded;

		// ── Lifecycle ─────────────────────────────────────────────────────────

		private void Awake()
		{
			LoadCampaignState();
			StartRun();
		}
		
		// ── Run management ────────────────────────────────────────────────────

		/// <summary>Resets run-scoped state and initialises stats to their starting values.</summary>
		public void StartRun()
		{
			stats.Clear();
			runFlags.Clear();
			visitCounts.Clear();
			lastSeenTurn.Clear();
			CurrentTurn = 0;

			// Initialise stats
			foreach (var stat in config.Stats)
			{
				stats[stat.statId] = stat.startingValue;
			}

			// Initialise run-scoped flags to defaults
			if (config.FlagRegistry != null)
			{
				foreach (var entry in config.FlagRegistry.AllFlags)
				{
					if (entry.Scope == Scope.Run)
						runFlags[entry.FlagId] = entry.DefaultValue;
				}
			}

			OnRunStarted?.Invoke();
		}

		/// <summary>Called when a stat hits a lethal value.</summary>
		public void EndRun(string causingStatId)
		{
			SaveCampaignState();
			OnStatLethal?.Invoke(causingStatId);
			OnRunEnded?.Invoke();
		}

		/// <summary>
		/// Increments the Current Turn Counter
		/// </summary>
		public void AdvanceTurn()
		{
			CurrentTurn++;
		}

		// ── Stat access ───────────────────────────────────────────────────────

		/// <summary>
		/// Returns a stat value, based on an ID
		/// </summary>
		/// <param name="statId">
		/// The ID of the stat to return
		/// </param>
		/// <returns>
		/// The value of the stat, based on the ID
		/// </returns>
		public int GetStat(string statId)
		{
			stats.TryGetValue(statId, out var val);
			return val;
		}

        /// <summary>
        /// Applies a delta to a stat. Clamps to the stat's range and checks for lethal values.
        /// Returns true if the stat became lethal.
        /// </summary>
        public bool ApplyStatDelta(string statId, int delta)
		{
			var def = GetStatDef(statId);
			if (def == null) return false;

			var oldVal = GetStat(statId);
			var newVal = def.Clamp(oldVal + delta);
			stats[statId] = newVal;

			OnStatChanged?.Invoke(statId, oldVal, newVal);

			if (def.IsLethal(newVal))
			{
				EndRun(statId);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Sets a stat directly (bypasses delta). Still clamps and checks lethality.
		/// </summary>
		public bool SetStat(string statId, int value)
		{
			var def = GetStatDef(statId);
			if (def == null) return false;

			var oldVal = GetStat(statId);
			var newVal = def.Clamp(value);
			stats[statId] = newVal;
			OnStatChanged?.Invoke(statId, oldVal, newVal);

			if (def.IsLethal(newVal))
			{
				EndRun(statId);
				return true;
			}
			return false;
		}

		// ── Flag access ───────────────────────────────────────────────────────

		/// <summary>
		/// Returns a flag value, based on an ID
		/// </summary>
		/// <param name="flagId">
		/// The ID of the flag to return
		/// </param>
		/// <returns>
		/// The value of the flag, based on the ID
		/// </returns>
		public int GetFlag(string flagId)
		{
			if (runFlags.TryGetValue(flagId, out var rv))
			{
				return rv;
			}
			return campaignFlags.TryGetValue(flagId, out var cv) ? cv : 0;
		}

		/// <summary>
		/// Executes the consequences of a <see cref="FlagEffect"/>
		/// </summary>
		/// <param name="effect">
		/// The flag to apply the effect of 
		/// </param>
		public void ApplyFlagEffect(FlagEffect effect)
		{
			if (string.IsNullOrEmpty(effect.FlagId)) return;

			var dict = GetFlagDict(effect.FlagId);
			dict.TryGetValue(effect.FlagId, out var current);

			var newVal = effect.effectType switch
			{
				FlagEffectType.Set => effect.value,
				FlagEffectType.Clear => 0,
				FlagEffectType.Increment => current + effect.value,
				FlagEffectType.Decrement => current - effect.value,
				_ => current
			};

			dict[effect.FlagId] = newVal;
			OnFlagChanged?.Invoke(effect.FlagId, newVal);
		}

		// ── Visit / cooldown tracking ─────────────────────────────────────────

		/// <summary>
		/// Retrieves the amount of times the card has been visited
		/// </summary>
		/// <param name="cardId">
		/// The ID of the card to query
		/// </param>
		/// <returns>
		/// The amount of times the player has visited this card
		/// </returns>
		public int GetVisitCount(string cardId)
		{
			visitCounts.TryGetValue(cardId, out var v);
			return v;
		}

		/// <summary>
		/// Makes record of the card that has been shown
		/// </summary>
		/// <param name="cardId">
		/// The ID of the card to record
		/// </param>
		public void RecordCardShown(string cardId)
		{
			visitCounts.TryGetValue(cardId, out var v);
			visitCounts[cardId] = v + 1;
			lastSeenTurn[cardId] = CurrentTurn;
		}

		/// <summary>
		/// Retrives the information regarding the previously viewed scene with this card
		/// </summary>
		/// <param name="cardId">
		/// The ID of the card to query
		/// </param>
		/// <returns>
		/// The turn previously viewed
		/// </returns>
		public int GetLastSeenTurn(string cardId)
		{
			lastSeenTurn.TryGetValue(cardId, out var t);
			return t;
		}

		/// <summary>
		/// Queries whether the card is available to show again
		/// </summary>
		/// <param name="card">
		/// The card to investigate
		/// </param>
		/// <returns>
		/// True if cooldown has expired, false otherwise
		/// </returns>
		public bool IsCooldownExpired(CardData card)
		{
			if (card.scheduling.CooldownTurns <= 0) return true;
			var lastSeen = GetLastSeenTurn(card.cardId);
			return CurrentTurn - lastSeen >= card.scheduling.CooldownTurns;
		}

		private void SaveCampaignState()
		{
			var data = new CampaignSaveData();
			foreach (var kvp in campaignFlags)
			{
				data.flagIds.Add(kvp.Key);
				data.flagValues.Add(kvp.Value);
			}
			PlayerPrefs.SetString(CampaignSaveKey, JsonUtility.ToJson(data));
			PlayerPrefs.Save();
		}

		private void LoadCampaignState()
		{
			campaignFlags.Clear();

			// Seed defaults first
			if (config.FlagRegistry != null)
			{
				foreach (var entry in config.FlagRegistry.AllFlags)
				{
					if (entry.Scope == Scope.Campaign)
						campaignFlags[entry.FlagId] = entry.DefaultValue;
				}
			}

			var json = PlayerPrefs.GetString(CampaignSaveKey, null);
			if (string.IsNullOrEmpty(json)) return;

			var data = JsonUtility.FromJson<CampaignSaveData>(json);
			for (var i = 0; i < data.flagIds.Count; i++)
				campaignFlags[data.flagIds[i]] = data.flagValues[i];
		}

		
		// TODO: this is atrocious.
		// Craft valid save system and correctly handle saving
		// Especially if we need to do cloud saving
		
		/// <summary>
		/// Delete the Save file
		/// </summary>
		public void ClearCampaignSave()
		{
			PlayerPrefs.DeleteKey(CampaignSaveKey);
			LoadCampaignState();
		}

		// ── Private helpers ───────────────────────────────────────────────────

		private StatDefinition GetStatDef(string statId)
		{
			foreach (var s in config.Stats)
			{
				if (s.statId == statId) return s;
			}
			Debug.LogWarning($"[GameStateManager] Unknown stat id: '{statId}'");
			return null;
		}

		private Dictionary<string, int> GetFlagDict(string flagId)
		{
			var entry = config.FlagRegistry?.GetEntry(flagId);
			return entry?.Scope == Scope.Campaign ? campaignFlags : runFlags;
		}

		[Serializable]
		private class CampaignSaveData
		{
			public List<string> flagIds = new();
			public List<int> flagValues = new();
		}
	}
}