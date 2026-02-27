using System;
using System.Collections.Generic;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Flags
{
	/// <summary>
	///     Central registry of all flag names used across the game.
	///     Designers register flags here; the inspector then shows dropdowns
	///     instead of free-text fields, preventing typo bugs.
	///     Create one instance and reference it from the GameConfig.
	/// </summary>
	[CreateAssetMenu(fileName = "FlagRegistry", menuName = "Reigns/Flag Registry")]
	public class Registry : ScriptableObject
	{

		public List<FlagEntry> flags = new();

		// ── Lookups ───────────────────────────────────────────────────────────

		private Dictionary<string, FlagEntry> _lookup;

		public IEnumerable<FlagEntry> AllFlags => flags;

		private void OnValidate()
		{
			_lookup = null;
			// rebuild on change
		}

		public FlagEntry GetEntry(string flagId)
		{
			BuildLookupIfNeeded();
			_lookup.TryGetValue(flagId, out var entry);
			return entry;
		}

		public bool Contains(string flagId)
		{
			BuildLookupIfNeeded();
			return _lookup.ContainsKey(flagId);
		}

		private void BuildLookupIfNeeded()
		{
			if (_lookup != null) return;
			_lookup = new Dictionary<string, FlagEntry>();
			foreach (var f in flags)
			{
				if (!string.IsNullOrEmpty(f.flagId))
					_lookup[f.flagId] = f;
			}
		}

		[Serializable]
		public class FlagEntry
		{
			[Tooltip("Unique key used in save data. Never rename after shipping.")]
			public string flagId;

			[Tooltip("Human-readable description for designers.")]
			public string description;

			[Tooltip("Run = resets on death. Campaign = persists across runs.")]
			public Scope scope = Scope.Run;

			[Tooltip("Default value at the start of a run (or campaign for campaign-scope flags).")]
			public int defaultValue;
		}
	}
}