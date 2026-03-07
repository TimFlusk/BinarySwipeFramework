using System;
using UnityEngine;
using VerdichotomyFramework.GameState;
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
		[SerializeField, Tooltip("Which stat to check.")]
		protected StatDefinition stat;

		[SerializeField, Tooltip("Minimum value (inclusive).")]
		protected int minValue;

		[SerializeField, Tooltip("Maximum value (inclusive).")]
		protected int maxValue = 100;

		public override bool Evaluate(GameStateManager state)
		{
			if (stat == null) return ApplyInvert(true);
			var v = state.GetStat(stat.statId);
			return ApplyInvert(v >= minValue && v <= maxValue);
		}
	}
}