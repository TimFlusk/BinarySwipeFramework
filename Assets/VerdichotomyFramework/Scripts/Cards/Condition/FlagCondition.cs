using System;
using UnityEngine;
using VerdichotomyFramework.GameState;
namespace VerdichotomyFramework.Cards.Condition
{
	/// <summary>
	/// Card only appears when a flag satisfies a comparison.
	/// </summary>
	[Serializable]
	public class FlagCondition : CardCondition
	{
		[Tooltip("Flag ID to evaluate (must exist in FlagRegistry).")]
		public string flagId;

		protected FlagCompareOperation operation = FlagCompareOperation.Equals;

		[Tooltip("Value to compare against.")]
		public int compareValue = 1;

		/// <inheritdoc />
		public override bool Evaluate(GameStateManager state)
		{
			if (string.IsNullOrEmpty(flagId)) return ApplyInvert(true);
			var v = state.GetFlag(flagId);
			var result = operation switch
			{
				FlagCompareOperation.Equals => v == compareValue,
				FlagCompareOperation.NotEquals => v != compareValue,
				FlagCompareOperation.GreaterThan => v > compareValue,
				FlagCompareOperation.LessThan => v < compareValue,
				FlagCompareOperation.GreaterOrEqual => v >= compareValue,
				FlagCompareOperation.LessOrEqual => v <= compareValue,
				_ => false
			};
			return ApplyInvert(result);
		}
	}
}