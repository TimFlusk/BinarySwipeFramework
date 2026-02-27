using System;
using System.Collections.Generic;
using UnityEngine;
using VerdichotomyFramework.Cards.Data;
using VerdichotomyFramework.Cards.Flags;
namespace VerdichotomyFramework
{
    /// <summary>
    ///     The single source of truth for all runtime state:
    ///     stat values, run flags, campaign flags, turn counter, and visit counts.
    ///     Persistence is handled here via a simple JSON save system.
    ///     Other systems read from and write to this manager; they never
    ///     cache state themselves.
    /// </summary>
    public class GameStateManager : MonoBehaviour
	{

		// ── Persistence ───────────────────────────────────────────────────────

		private const string CampaignSaveKey = "VerdichotomyFramework_Campaign";
		// ── Dependencies ──────────────────────────────────────────────────────

		[Tooltip("The game configuration asset.")]
		public GameConfig config;
		private readonly Dictionary<string, int> _campaignFlags = new();

		private readonly Dictionary<string, int> _lastSeenTurn = new(); // cardId → turn last shown
		private readonly Dictionary<string, int> _runFlags = new();

		// ── State ─────────────────────────────────────────────────────────────

		private readonly Dictionary<string, int> _stats = new();
		private readonly Dictionary<string, int> _visitCounts = new(); // cardId → visit count

		public int CurrentTurn { get; private set; }

		// ── Lifecycle ─────────────────────────────────────────────────────────

		private void Awake()
		{
			LoadCampaignState();
			StartRun();
		}

		// ── Events ────────────────────────────────────────────────────────────

		public event Action<string, int, int> OnStatChanged; // statId, oldValue, newValue
		public event Action<string> OnStatLethal; // statId
		public event Action<string, int> OnFlagChanged; // flagId, newValue
		public event Action OnRunStarted;
		public event Action OnRunEnded;

		// ── Run management ────────────────────────────────────────────────────

		/// <summary>Resets run-scoped state and initialises stats to their starting values.</summary>
		public void StartRun()
		{
			_stats.Clear();
			_runFlags.Clear();
			_visitCounts.Clear();
			_lastSeenTurn.Clear();
			CurrentTurn = 0;

			// Initialise stats
			foreach (var stat in config.stats)
			{
				_stats[stat.statId] = stat.startingValue;
			}

			// Initialise run-scoped flags to defaults
			if (config.flagRegistry != null)
			{
				foreach (var entry in config.flagRegistry.AllFlags)
				{
					if (entry.scope == Scope.Run)
						_runFlags[entry.flagId] = entry.defaultValue;
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

		public void AdvanceTurn()
		{
			CurrentTurn++;
		}

		// ── Stat access ───────────────────────────────────────────────────────

		public int GetStat(string statId)
		{
			_stats.TryGetValue(statId, out var val);
			return val;
		}

        /// <summary>
        ///     Applies a delta to a stat. Clamps to the stat's range and checks for lethal values.
        ///     Returns true if the stat became lethal.
        /// </summary>
        public bool ApplyStatDelta(string statId, int delta)
		{
			var def = GetStatDef(statId);
			if (def == null) return false;

			var oldVal = GetStat(statId);
			var newVal = def.Clamp(oldVal + delta);
			_stats[statId] = newVal;

			OnStatChanged?.Invoke(statId, oldVal, newVal);

			if (def.IsLethal(newVal))
			{
				EndRun(statId);
				return true;
			}
			return false;
		}

		/// <summary>Sets a stat directly (bypasses delta). Still clamps and checks lethality.</summary>
		public bool SetStat(string statId, int value)
		{
			var def = GetStatDef(statId);
			if (def == null) return false;

			var oldVal = GetStat(statId);
			var newVal = def.Clamp(value);
			_stats[statId] = newVal;
			OnStatChanged?.Invoke(statId, oldVal, newVal);

			if (def.IsLethal(newVal))
			{
				EndRun(statId);
				return true;
			}
			return false;
		}

		// ── Flag access ───────────────────────────────────────────────────────

		public int GetFlag(string flagId)
		{
			if (_runFlags.TryGetValue(flagId, out var rv)) return rv;
			if (_campaignFlags.TryGetValue(flagId, out var cv)) return cv;
			return 0;
		}

		public void ApplyFlagEffect(FlagEffect effect)
		{
			if (string.IsNullOrEmpty(effect.flagId)) return;

			var dict = GetFlagDict(effect.flagId);
			dict.TryGetValue(effect.flagId, out var current);

			var newVal = effect.effectType switch
			{
				FlagEffectType.Set => effect.value,
				FlagEffectType.Clear => 0,
				FlagEffectType.Increment => current + effect.value,
				FlagEffectType.Decrement => current - effect.value,
				_ => current
			};

			dict[effect.flagId] = newVal;
			OnFlagChanged?.Invoke(effect.flagId, newVal);
		}

		// ── Visit / cooldown tracking ─────────────────────────────────────────

		public int GetVisitCount(string cardId)
		{
			_visitCounts.TryGetValue(cardId, out var v);
			return v;
		}

		public void RecordCardShown(string cardId)
		{
			_visitCounts.TryGetValue(cardId, out var v);
			_visitCounts[cardId] = v + 1;
			_lastSeenTurn[cardId] = CurrentTurn;
		}

		public int GetLastSeenTurn(string cardId)
		{
			_lastSeenTurn.TryGetValue(cardId, out var t);
			return t;
		}

		public bool IsCooldownExpired(CardData card)
		{
			if (card.scheduling.cooldownTurns <= 0) return true;
			var lastSeen = GetLastSeenTurn(card.cardId);
			return CurrentTurn - lastSeen >= card.scheduling.cooldownTurns;
		}

		private void SaveCampaignState()
		{
			var data = new CampaignSaveData();
			foreach (var kvp in _campaignFlags)
			{
				data.flagIds.Add(kvp.Key);
				data.flagValues.Add(kvp.Value);
			}
			PlayerPrefs.SetString(CampaignSaveKey, JsonUtility.ToJson(data));
			PlayerPrefs.Save();
		}

		private void LoadCampaignState()
		{
			_campaignFlags.Clear();

			// Seed defaults first
			if (config.flagRegistry != null)
			{
				foreach (var entry in config.flagRegistry.AllFlags)
				{
					if (entry.scope == Scope.Campaign)
						_campaignFlags[entry.flagId] = entry.defaultValue;
				}
			}

			var json = PlayerPrefs.GetString(CampaignSaveKey, null);
			if (string.IsNullOrEmpty(json)) return;

			var data = JsonUtility.FromJson<CampaignSaveData>(json);
			for (var i = 0; i < data.flagIds.Count; i++)
				_campaignFlags[data.flagIds[i]] = data.flagValues[i];
		}

		public void ClearCampaignSave()
		{
			PlayerPrefs.DeleteKey(CampaignSaveKey);
			LoadCampaignState();
		}

		// ── Private helpers ───────────────────────────────────────────────────

		private StatDefinition GetStatDef(string statId)
		{
			foreach (var s in config.stats)
			{
				if (s.statId == statId) return s;
			}
			Debug.LogWarning($"[GameStateManager] Unknown stat id: '{statId}'");
			return null;
		}

		private Dictionary<string, int> GetFlagDict(string flagId)
		{
			var entry = config.flagRegistry?.GetEntry(flagId);
			return entry?.scope == Scope.Campaign ? _campaignFlags : _runFlags;
		}

		[Serializable]
		private class CampaignSaveData
		{
			public List<string> flagIds = new();
			public List<int> flagValues = new();
		}
	}
}