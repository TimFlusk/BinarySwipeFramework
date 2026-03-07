using System;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Flags
{
	[Serializable]
	public class FlagEntry
	{
		[Tooltip("Unique key used in save data. Never rename after shipping.")]
		public string FlagId;

		[Tooltip("Human-readable description for designers.")]
		public string Description;

		[Tooltip("Run = resets on death. Campaign = persists across runs.")]
		public Scope Scope = Scope.Run;

		[Tooltip("Default value at the start of a run (or campaign for campaign-scope flags).")]
		public int DefaultValue;
	}
}