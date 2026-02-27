using System;
using ReignsFramework.Runtime;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Condition
{
	// ─────────────────────────────────────────────────────────────────────────
	// Stat range condition
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Card only appears when a stat is within a given range.
	/// </summary>
	[Serializable]
	public class StatRangeCondition : CardCondition
	{
		[Tooltip("Which stat to check.")]
		public StatDefinition stat;

		[Tooltip("Minimum value (inclusive).")]
		public int minValue;

		[Tooltip("Maximum value (inclusive).")]
		public int maxValue = 100;

		public override bool Evaluate(GameStateManager state)
		{
			if (stat == null) return ApplyInvert(true);
			var v = state.GetStat(stat.statId);
			return ApplyInvert(v >= minValue && v <= maxValue);
		}
	}
}