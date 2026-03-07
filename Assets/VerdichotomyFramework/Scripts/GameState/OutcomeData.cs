using System;
using System.Runtime.Serialization;
using UnityEngine;
using VerdichotomyFramework.Cards;
using VerdichotomyFramework.Cards.Condition;
using VerdichotomyFramework.Cards.Data;
namespace VerdichotomyFramework.GameState
{
	// ─────────────────────────────────────────────────────────────────────────
	// Stat delta
	// ─────────────────────────────────────────────────────────────────────────

	[Serializable]
	public class StatDelta
	{
		[field: SerializeField, DataMember, Tooltip("Which stat to modify.")]
		public StatDefinition Stat { get; private set; }

		[field: SerializeField, DataMember, Tooltip("Amount to add (use negative to subtract).")]
		public int Delta { get; private set; }
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Flag effect
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Explicit defining factor for Flag Effect
	/// </summary>
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
		[field: SerializeField, DataMember, Tooltip("Flag to affect (must exist in FlagRegistry).")]
		public string FlagId { get; private set; }

		public FlagEffectType effectType = FlagEffectType.Set;

		[field: SerializeField, DataMember, Tooltip("Value used for Set and Increment/Decrement operations.")]
		public int value { get; private set; } = 1;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Card scheduling effect
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Qualitative changes to the game state to allow more expressive card consequences
	/// </summary>
	public enum CardEffectType
	{
		/// <summary>
		/// This card will be the next card shown, overriding the scheduler.
		/// </summary>
		ForceNext,
		
		/// <summary>
		/// Add a card to the priority queue (shown before normal pool draws).
		/// </summary>
		QueueWithPriority,
		
		/// <summary>
		/// Enable a card pool so its cards become eligible.
		/// </summary>
		EnablePool,
		
		/// <summary>
		/// Disable a card pool so none of its cards appear.
		/// </summary>
		DisablePool,
		
		/// <summary>
		/// Remove a specific card permanently from the current run.
		/// </summary>
		RemoveCardFromRun
	}

	/// <summary>
	/// Representation of the effect of card effect as it changes game state
	/// </summary>
	[Serializable]
	public class CardSchedulingEffect
	{
		[field: SerializeField, DataMember]
		public CardEffectType EffectType  { get; private set; }

		[field: SerializeField, DataMember, Tooltip("Target card (for ForceNext, QueueWithPriority, RemoveCardFromRun).")]
		public CardData TargetCard { get; private set; }

		[field: SerializeField, DataMember, Tooltip("Target pool (for EnablePool, DisablePool).")]
		public CardPoolData TargetPool { get; private set; }
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Conditional stat delta override
	// ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces the base stat deltas when all conditions are met.
    /// Allows a single outcome to behave differently based on game state.
    /// e.g. "If the player has built the castle, this gives +10 Army instead of +5."
    /// </summary>
    [Serializable]
	public class ConditionalDeltaOverride
	{
		[Tooltip("All conditions must pass for this override to apply."), SerializeReference]
		public CardCondition[] Conditions = Array.Empty<CardCondition>();

		[Tooltip("These deltas replace (not add to) the base deltas when active.")]
		public StatDelta[] OverrideDelta = Array.Empty<StatDelta>();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Outcome data — one side of a card (left or right swipe)
	// ─────────────────────────────────────────────────────────────────────────

	[Serializable]
	public class OutcomeData
	{
		[field: SerializeField, DataMember, Header("Presentation"), Tooltip("Short text shown when swiping in this direction (hover / in-progress).")]
		public string SwipeHintText { get; private set; }

		[field: SerializeField, DataMember, Tooltip("Text shown after the player commits to this outcome."), TextArea(2, 5)]
		public string ResponseText { get; private set; }

		[field: SerializeField, DataMember, Header("Stat Effects"), Tooltip("Stat changes applied when this outcome is chosen.")]
		public StatDelta[] StatDeltas { get; private set; } = Array.Empty<StatDelta>();

		[field: SerializeField, DataMember, Tooltip("Override the base deltas when certain conditions are met. First matching override wins.")]
		public ConditionalDeltaOverride[] ConditionalOverrides { get; private set; } = Array.Empty<ConditionalDeltaOverride>();

		[field: SerializeField, DataMember, Header("Flag Effects"), Tooltip("Flags to set, clear, or increment when this outcome is chosen.")]
		public FlagEffect[] FlagEffects { get; private set; } = Array.Empty<FlagEffect>();

		[field: SerializeField, DataMember, Header("Scheduling Effects"), Tooltip("Cards or pools to affect when this outcome is chosen.")]
		public CardSchedulingEffect[] SchedulingEffects { get; private set; } = Array.Empty<CardSchedulingEffect>();

		// ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        ///     Returns the effective stat deltas, applying any matching conditional overrides.
        /// </summary>
        public StatDelta[] ResolveDeltas(GameStateManager state)
		{
			foreach (var o in ConditionalOverrides)
			{
				var allPass = true;
				foreach (var c in o.Conditions)
				{
					if (c == null || !c.Evaluate(state))
					{
						allPass = false;
						break;
					}
				}
				if (allPass) return o.OverrideDelta;
			}
			return StatDeltas;
		}
	}
}