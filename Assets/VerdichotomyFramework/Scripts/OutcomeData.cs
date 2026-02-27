using System;
using ReignsFramework.Runtime;
using UnityEngine;
using VerdichotomyFramework.Cards;
namespace VerdichotomyFramework
{
	// ─────────────────────────────────────────────────────────────────────────
	// Stat delta
	// ─────────────────────────────────────────────────────────────────────────

	[Serializable]
	public class StatDelta
	{
		[Tooltip("Which stat to modify.")]
		public StatDefinition stat;

		[Tooltip("Amount to add (use negative to subtract).")]
		public int delta;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Flag effect
	// ─────────────────────────────────────────────────────────────────────────

	public enum FlagEffectType
	{
		Set,
		Clear,
		Increment,
		Decrement
	}

	[Serializable]
	public class FlagEffect
	{
		[Tooltip("Flag to affect (must exist in FlagRegistry).")]
		public string flagId;

		public FlagEffectType effectType = FlagEffectType.Set;

		[Tooltip("Value used for Set and Increment/Decrement operations.")]
		public int value = 1;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Card scheduling effect
	// ─────────────────────────────────────────────────────────────────────────

	public enum CardEffectType
	{
		/// <summary>This card will be the next card shown, overriding the scheduler.</summary>
		ForceNext,
		/// <summary>Add a card to the priority queue (shown before normal pool draws).</summary>
		QueueWithPriority,
		/// <summary>Enable a card pool so its cards become eligible.</summary>
		EnablePool,
		/// <summary>Disable a card pool so none of its cards appear.</summary>
		DisablePool,
		/// <summary>Remove a specific card permanently from the current run.</summary>
		RemoveCardFromRun
	}

	[Serializable]
	public class CardSchedulingEffect
	{
		public CardEffectType effectType;

		[Tooltip("Target card (for ForceNext, QueueWithPriority, RemoveCardFromRun).")]
		public CardData targetCard;

		[Tooltip("Target pool (for EnablePool, DisablePool).")]
		public CardPoolData targetPool;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Conditional stat delta override
	// ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Replaces the base stat deltas when all conditions are met.
    ///     Allows a single outcome to behave differently based on game state.
    ///     e.g. "If the player has built the castle, this gives +10 Army instead of +5."
    /// </summary>
    [Serializable]
	public class ConditionalDeltaOverride
	{
		[Tooltip("All conditions must pass for this override to apply."), SerializeReference]
		public CardCondition[] conditions = Array.Empty<CardCondition>();

		[Tooltip("These deltas replace (not add to) the base deltas when active.")]
		public StatDelta[] overrideDeltas = Array.Empty<StatDelta>();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Outcome data — one side of a card (left or right swipe)
	// ─────────────────────────────────────────────────────────────────────────

	[Serializable]
	public class OutcomeData
	{
		[Header("Presentation"), Tooltip("Short text shown when swiping in this direction (hover / in-progress).")]
		public string swipeHintText;

		[Tooltip("Text shown after the player commits to this outcome."), TextArea(2, 5)]
		public string responseText;

		[Header("Stat Effects"), Tooltip("Stat changes applied when this outcome is chosen.")]
		public StatDelta[] statDeltas = Array.Empty<StatDelta>();

		[Tooltip("Override the base deltas when certain conditions are met. First matching override wins.")]
		public ConditionalDeltaOverride[] conditionalOverrides = Array.Empty<ConditionalDeltaOverride>();

		[Header("Flag Effects"), Tooltip("Flags to set, clear, or increment when this outcome is chosen.")]
		public FlagEffect[] flagEffects = Array.Empty<FlagEffect>();

		[Header("Scheduling Effects"), Tooltip("Cards or pools to affect when this outcome is chosen.")]
		public CardSchedulingEffect[] schedulingEffects = Array.Empty<CardSchedulingEffect>();

		// ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        ///     Returns the effective stat deltas, applying any matching conditional overrides.
        /// </summary>
        public StatDelta[] ResolveDeltas(GameStateManager state)
		{
			foreach (var o in conditionalOverrides)
			{
				var allPass = true;
				foreach (var c in o.conditions)
				{
					if (c == null || !c.Evaluate(state))
					{
						allPass = false;
						break;
					}
				}
				if (allPass) return o.overrideDeltas;
			}
			return statDeltas;
		}
	}
}