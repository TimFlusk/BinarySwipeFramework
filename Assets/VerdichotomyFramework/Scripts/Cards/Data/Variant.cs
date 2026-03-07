using System;
using System.Runtime.Serialization;
using UnityEngine;
using VerdichotomyFramework.GameState;
namespace VerdichotomyFramework.Cards.Data
{
	// ─────────────────────────────────────────────────────────────────────────
	// Variant — used when recurrence = Cycling
	// ─────────────────────────────────────────────────────────────────────────
	
	/// <summary>
	/// A variant is an alternate version of a card shown on subsequent visits.
	/// e.g. first time: "The merchant arrives.", second time: "The merchant returns."
	/// The base card's prompt/outcomes are used for visit 0; variants cover visits 1+.
	/// </summary>
	[Serializable, DataContract]
	public class Variant
	{
		[field: SerializeField, DataMember]
		[Tooltip("Prompt text for this visit number."), TextArea(2, 5)]
		public string PromptText { get; private set; }

		[field: SerializeField, DataMember]
		[Tooltip("Override the left outcome for this variant (leave null to use the base outcome).")]
		public OutcomeData LeftOutcomeOverride { get; private set; }

		[field: SerializeField, DataMember]
		[Tooltip("Override the right outcome for this variant (leave null to use the base outcome).")]
		public OutcomeData RightOutcomeOverride { get; private set; }
	}
}