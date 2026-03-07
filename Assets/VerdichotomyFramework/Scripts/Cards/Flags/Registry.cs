using System;
using System.Collections.Generic;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Flags
{
	/// <summary>
	/// Central registry of all flag names used across the game.
	/// Designers register flags here; the inspector then shows dropdowns
	/// instead of free-text fields, preventing typo bugs.
	/// Create one instance and reference it from the GameConfig.
	/// </summary>
	[CreateAssetMenu(fileName = "FlagRegistry", menuName = "Verdichotomy/Flag Registry")]
	public class Registry : ScriptableObject
	{

		/// <summary>
		/// Collection of flags
		/// </summary>
		[SerializeField]
		private List<FlagEntry> flags = new();
		public IEnumerable<FlagEntry> AllFlags => flags;


		private Dictionary<string, FlagEntry> _lookup;

		/// <summary>
		/// Attempts to find a flag based on id.
		/// </summary>
		/// <param name="flagId">
		/// The id of the flag to be searching for.
		/// </param>
		/// <param name="entry">
		/// The reference to the flag entry if it is found
		/// </param>
		/// <returns>
		/// <see langword="True"/>: If the entry was found. <see langword="False"/>: If no entry associated with <param name="flagId">flag id</param> was found.
		/// </returns>
		public bool TryGetEntry(string flagId, out FlagEntry entry)
		{
			entry = GetEntry(flagId);
			return entry != null;
		}
		
		
		/// <summary>
		/// Returns a flag based on the id
		/// </summary>
		/// <param name="flagId">The id of the flag to search for</param>
		/// <returns>
		/// The <see cref="FlagEntry"/> if it exists
		/// </returns>
		public FlagEntry GetEntry(string flagId)
		{
			BuildLookupIfNeeded();
			_lookup.TryGetValue(flagId, out var entry);
			return entry;
		}

		/// <summary>
		/// Verifies if flagId is present
		/// </summary>
		/// <param name="flagId">
		/// The flag ID to compare against
		/// </param>
		/// <returns>
		///  <see langword="True"/> If the flag entry is present, <see langword="False"/> otherwise.
		/// </returns>
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
				if (!string.IsNullOrEmpty(f.FlagId))
					_lookup[f.FlagId] = f;
			}
		}
		
#if UNITY_EDITOR
		private void OnValidate()
		{
			_lookup = null;
			BuildLookupIfNeeded();
		}
#endif
	}
}