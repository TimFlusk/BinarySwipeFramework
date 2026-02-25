using System;
using ReignsFramework.Runtime;
using UnityEngine;
namespace VerdichotomyFramework
{
	// ─────────────────────────────────────────────────────────────────────────
	// Scheduling settings embedded in CardData
	// ─────────────────────────────────────────────────────────────────────────

	public enum CardRecurrence
	{
		/// <summary>Shown once, then removed from the pool forever (this run).</summary>
		OneShot,
		/// <summary>Can appear any number of times, subject to cooldown.</summary>
		Repeatable,
		/// <summary>Cycles through variants sequentially (see CardData.variants).</summary>
		Cycling
	}

	[Serializable]
	public class CardSchedulingData
	{
		[Header("Recurrence")]
		public CardRecurrence recurrence = CardRecurrence.Repeatable;

		[Tooltip("Minimum number of turns before this card can appear again. 0 = no cooldown.")]
		public int cooldownTurns;

		[Header("Priority"),
		 Tooltip("Higher weight = more likely to be chosen when multiple cards are eligible. Relative to other cards in the same pool."),
		 Range(1, 100)]
		public int weight = 10;

		[Tooltip(
			"If true, this card bypasses normal pool selection and goes to the front of the priority queue automatically when its conditions are met.")]
		public bool forcePriority;

		[Header("Appearance Conditions"),
		 Tooltip("All conditions must pass for this card to be eligible. Uses [SerializeReference] for polymorphism."), SerializeReference]
		public CardCondition[] conditions = Array.Empty<CardCondition>();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Variant — used when recurrence = Cycling
	// ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     A variant is an alternate version of a card shown on subsequent visits.
    ///     e.g. first time: "The merchant arrives.", second time: "The merchant returns."
    ///     The base card's prompt/outcomes are used for visit 0; variants cover visits 1+.
    /// </summary>
    [Serializable]
	public class CardVariant
	{
		[Tooltip("Prompt text for this visit number."), TextArea(2, 5)]
		public string promptText;

		[Tooltip("Override the left outcome for this variant (leave null to use the base outcome).")]
		public OutcomeData leftOutcomeOverride;

		[Tooltip("Override the right outcome for this variant (leave null to use the base outcome).")]
		public OutcomeData rightOutcomeOverride;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// CardData — the main designer-facing ScriptableObject
	// ─────────────────────────────────────────────────────────────────────────

	[CreateAssetMenu(fileName = "NewCard", menuName = "Reigns/Card")]
	public class CardData : ScriptableObject
	{
		[Header("Identity"), Tooltip("Unique ID. Used in save data and scheduling effects. Never change after shipping.")]
		public string cardId;

		[Header("Presentation"), Tooltip("The character presenting this card.")]
		public CharacterData character;

		[Tooltip("Main situation text shown on the card."), TextArea(3, 8)]
		public string promptText;

		[Header("Outcomes"), Tooltip("What happens when the player swipes left.")]
		public OutcomeData leftOutcome;

		[Tooltip("What happens when the player swipes right.")]
		public OutcomeData rightOutcome;

		[Header("Scheduling")]
		public CardSchedulingData scheduling;

		[Header("Variants (Cycling only)"),
		 Tooltip("Shown on 2nd, 3rd, ... visits when recurrence = Cycling. Loops back to last if visits exceed variant count.")]
		public CardVariant[] variants = Array.Empty<CardVariant>();

#if UNITY_EDITOR
		private void OnValidate()
		{
			// Auto-populate cardId from asset name if blank.
			if (string.IsNullOrEmpty(cardId))
				cardId = name;
		}
#endif

		// ── Runtime helpers ───────────────────────────────────────────────────

        /// <summary>
        ///     Gets the prompt text for a given visit count (0-indexed).
        ///     Visit 0 = base card. Visit 1+ = variants (clamped to last variant).
        /// </summary>
        public string GetPromptForVisit(int visitCount)
		{
			if (visitCount == 0 || variants.Length == 0) return promptText;
			var idx = Mathf.Clamp(visitCount - 1, 0, variants.Length - 1);
			return variants[idx].promptText;
		}

        /// <summary>
        ///     Gets the effective left outcome for a given visit count.
        ///     Returns a variant override if one exists, otherwise the base outcome.
        /// </summary>
        public OutcomeData GetLeftOutcomeForVisit(int visitCount)
		{
			if (visitCount > 0 && variants.Length > 0)
			{
				var idx = Mathf.Clamp(visitCount - 1, 0, variants.Length - 1);
				if (variants[idx].leftOutcomeOverride != null)
					return variants[idx].leftOutcomeOverride;
			}
			return leftOutcome;
		}

        /// <summary>
        ///     Gets the effective right outcome for a given visit count.
        ///     Returns a variant override if one exists, otherwise the base outcome.
        /// </summary>
        public OutcomeData GetRightOutcomeForVisit(int visitCount)
		{
			if (visitCount > 0 && variants.Length > 0)
			{
				var idx = Mathf.Clamp(visitCount - 1, 0, variants.Length - 1);
				if (variants[idx].rightOutcomeOverride != null)
					return variants[idx].rightOutcomeOverride;
			}
			return rightOutcome;
		}

        /// <summary>
        ///     Returns true if all scheduling conditions pass.
        /// </summary>
        public bool AreConditionsMet(GameStateManager state)
		{
			foreach (var c in scheduling.conditions)
			{
				if (c != null && !c.Evaluate(state)) return false;
			}
			return true;
		}
	}
}