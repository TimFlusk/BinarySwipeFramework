using System;
using UnityEngine;
using VerdichotomyFramework.GameState;
namespace VerdichotomyFramework.Cards.Condition
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
	// Compound conditions (AND / OR)
	// ─────────────────────────────────────────────────────────────────────────

	// Note: Unity doesn't serialise abstract polymorphic lists out-of-the-box
	// without a custom property drawer or [SerializeReference].
	// These use [SerializeReference] — Unity 2019.3+.

	

	
}