using System;
using ReignsFramework.Runtime;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Condition
{
	// ─────────────────────────────────────────────────────────────────────────
	// Turn / year range condition
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Card only appears within a turn number range (0 = no limit).
	/// </summary>
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
}