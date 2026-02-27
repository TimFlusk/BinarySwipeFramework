using System;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Data
{
	// ─────────────────────────────────────────────────────────────────────────
	// Variant — used when recurrence = Cycling
	// ─────────────────────────────────────────────────────────────────────────
	
	/// <summary>
	///     A variant is an alternate version of a card shown on subsequent visits.
	///     e.g. first time: "The merchant arrives.", second time: "The merchant returns."
	///     The base card's prompt/outcomes are used for visit 0; variants cover visits 1+.
	/// </summary>
	[Serializable]
	public class Variant
	{
		[Tooltip("Prompt text for this visit number."), TextArea(2, 5)]
		public string promptText;

		[Tooltip("Override the left outcome for this variant (leave null to use the base outcome).")]
		public OutcomeData leftOutcomeOverride;

		[Tooltip("Override the right outcome for this variant (leave null to use the base outcome).")]
		public OutcomeData rightOutcomeOverride;
	}
}