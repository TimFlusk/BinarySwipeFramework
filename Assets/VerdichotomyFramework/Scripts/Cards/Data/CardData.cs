using System;
using UnityEngine;
using VerdichotomyFramework.Characters;
using VerdichotomyFramework.GameState;
namespace VerdichotomyFramework.Cards.Data
{
	// ─────────────────────────────────────────────────────────────────────────
	// CardData — the main designer-facing ScriptableObject
	// ─────────────────────────────────────────────────────────────────────────

	[CreateAssetMenu(fileName = "NewCard", menuName = "Reigns/Card")]
	public class CardData : ScriptableObject
	{
		[field: SerializeField, Header("Identity"), Tooltip("Unique ID. Used in save data and scheduling effects. Never change after shipping.")]
		public string cardId { get; private set; }

		[field: SerializeField, Header("Presentation"), Tooltip("The character presenting this card.")]
		public CharacterData character { get; private set; }

		[field: SerializeField, Tooltip("Main situation text shown on the card."), TextArea(3, 8)]
		public string promptText { get; private set; }

		[field: SerializeField, Header("Outcomes"), Tooltip("What happens when the player swipes left.")]
		public OutcomeData leftOutcome { get; private set; }

		[field: SerializeField, Tooltip("What happens when the player swipes right.")]
		public OutcomeData rightOutcome { get; private set; }

		[field: SerializeField, Header("Scheduling")]
		public SchedulingData scheduling { get; private set; }

		[field: SerializeField, Header("Variants (Cycling only)"),
		        Tooltip("Shown on 2nd, 3rd, ... visits when recurrence = Cycling. Loops back to last if visits exceed variant count.")]
		public Variant[] variants { get; private set; } = Array.Empty<Variant>();

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
        /// Gets the prompt text for a given visit count (0-indexed).
        /// Visit 0 = base card. Visit 1+ = variants (clamped to last variant).
        /// </summary>
        public string GetPromptForVisit(int visitCount)
		{
			if (visitCount == 0 || variants.Length == 0) return promptText;
			var idx = Mathf.Clamp(visitCount - 1, 0, variants.Length - 1);
			return variants[idx].PromptText;
		}

        /// <summary>
        /// Gets the effective left outcome for a given visit count.
        /// Returns a variant override if one exists, otherwise the base outcome.
        /// </summary>
        public OutcomeData GetLeftOutcomeForVisit(int visitCount)
		{
			if (visitCount > 0 && variants.Length > 0)
			{
				var idx = Mathf.Clamp(visitCount - 1, 0, variants.Length - 1);
				if (variants[idx].LeftOutcomeOverride != null)
					return variants[idx].LeftOutcomeOverride;
			}
			return leftOutcome;
		}

        /// <summary>
        /// Gets the effective right outcome for a given visit count.
        /// Returns a variant override if one exists, otherwise the base outcome.
        /// </summary>
        public OutcomeData GetRightOutcomeForVisit(int visitCount)
		{
			if (visitCount > 0 && variants.Length > 0)
			{
				var idx = Mathf.Clamp(visitCount - 1, 0, variants.Length - 1);
				if (variants[idx].RightOutcomeOverride != null)
					return variants[idx].RightOutcomeOverride;
			}
			return rightOutcome;
		}

        /// <summary>
        /// Returns true if all scheduling conditions pass.
        /// </summary>
        public bool AreConditionsMet(GameStateManager state)
		{
			foreach (var c in scheduling.Conditions)
			{
				if (c != null && !c.Evaluate(state)) return false;
			}
			return true;
		}
	}
}