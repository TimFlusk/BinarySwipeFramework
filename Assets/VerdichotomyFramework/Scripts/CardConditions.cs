using System;
using ReignsFramework.Runtime;
using UnityEngine;
namespace VerdichotomyFramework
{
	// ─────────────────────────────────────────────────────────────────────────
	// Base condition
	// ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Abstract base for all conditions that gate whether a card can appear.
    ///     Subclass this to add new condition types. Mark with [Serializable] so
    ///     Unity serialises them inline inside CardData.
    /// </summary>
    [Serializable]
	public abstract class CardCondition
	{
		[Tooltip("Invert this condition (NOT logic).")]
		public bool invert;

		/// <summary>
		/// Override to implement the actual check.
		/// </summary>
		public abstract bool Evaluate(GameStateManager state);

		protected bool ApplyInvert(bool result)
		{
			return invert ? !result : result;
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Stat range condition
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>Card only appears when a stat is within a given range.</summary>
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

	// ─────────────────────────────────────────────────────────────────────────
	// Flag condition
	// ─────────────────────────────────────────────────────────────────────────

	public enum FlagCompareOp
	{
		Equals,
		NotEquals,
		GreaterThan,
		LessThan,
		GreaterOrEqual,
		LessOrEqual
	}

	/// <summary>Card only appears when a flag satisfies a comparison.</summary>
	[Serializable]
	public class FlagCondition : CardCondition
	{
		[Tooltip("Flag ID to evaluate (must exist in FlagRegistry).")]
		public string flagId;

		public FlagCompareOp operation = FlagCompareOp.Equals;

		[Tooltip("Value to compare against.")]
		public int compareValue = 1;

		public override bool Evaluate(GameStateManager state)
		{
			if (string.IsNullOrEmpty(flagId)) return ApplyInvert(true);
			var v = state.GetFlag(flagId);
			var result = operation switch
			{
				FlagCompareOp.Equals => v == compareValue,
				FlagCompareOp.NotEquals => v != compareValue,
				FlagCompareOp.GreaterThan => v > compareValue,
				FlagCompareOp.LessThan => v < compareValue,
				FlagCompareOp.GreaterOrEqual => v >= compareValue,
				FlagCompareOp.LessOrEqual => v <= compareValue,
				_ => false
			};
			return ApplyInvert(result);
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Turn / year range condition
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>Card only appears within a turn number range (0 = no limit).</summary>
	[Serializable]
	public class TurnRangeCondition : CardCondition
	{
		[Tooltip("First turn this card can appear. 0 = from the start.")]
		public int firstTurn;

		[Tooltip("Last turn this card can appear. 0 = no upper limit.")]
		public int lastTurn;

		public override bool Evaluate(GameStateManager state)
		{
			var turn = state.CurrentTurn;
			if (firstTurn > 0 && turn < firstTurn) return ApplyInvert(false);
			if (lastTurn > 0 && turn > lastTurn) return ApplyInvert(false);
			return ApplyInvert(true);
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Compound conditions (AND / OR)
	// ─────────────────────────────────────────────────────────────────────────

	// Note: Unity doesn't serialise abstract polymorphic lists out-of-the-box
	// without a custom property drawer or [SerializeReference].
	// These use [SerializeReference] — Unity 2019.3+.

	/// <summary>All child conditions must be true.</summary>
	[Serializable]
	public class AndCondition : CardCondition
	{
		[SerializeReference]
		public CardCondition[] conditions = Array.Empty<CardCondition>();

		public override bool Evaluate(GameStateManager state)
		{
			foreach (var c in conditions)
			{
				if (c != null && !c.Evaluate(state)) return ApplyInvert(false);
			}
			return ApplyInvert(true);
		}
	}

	/// <summary>At least one child condition must be true.</summary>
	[Serializable]
	public class OrCondition : CardCondition
	{
		[SerializeReference]
		public CardCondition[] conditions = Array.Empty<CardCondition>();

		public override bool Evaluate(GameStateManager state)
		{
			foreach (var c in conditions)
			{
				if (c != null && c.Evaluate(state)) return ApplyInvert(true);
			}
			return ApplyInvert(false);
		}
	}
}