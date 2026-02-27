using System;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Data
{
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
		public SchedulingData scheduling;

		[Header("Variants (Cycling only)"),
		 Tooltip("Shown on 2nd, 3rd, ... visits when recurrence = Cycling. Loops back to last if visits exceed variant count.")]
		public Variant[] variants = Array.Empty<Variant>();

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