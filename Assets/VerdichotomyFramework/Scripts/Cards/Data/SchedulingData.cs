using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using VerdichotomyFramework.Cards.Condition;
namespace VerdichotomyFramework.Cards.Data
{
	// ─────────────────────────────────────────────────────────────────────────
	// Scheduling settings embedded in CardData
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Collection of definitions for indicating how to schedule a particular card
	/// </summary>
	[Serializable, DataContract]
	public class SchedulingData
	{
		[DataMember]
		[field: SerializeField, Header("Recurrence")]
		public Recurrence Recurrence { get; private set; } = Recurrence.Repeatable;

		[DataMember]
		[field: SerializeField, Tooltip("Minimum number of turns before this card can appear again. 0 = no cooldown.")]
		public int CooldownTurns { get; private set; }

		[DataMember]
		[field: SerializeField, Header("Priority"),
		        Tooltip("Higher weight = more likely to be chosen when multiple cards are eligible. Relative to other cards in the same pool."),
		        Range(1, 100)]
		public int Weight { get; private set; } = 10;

		[DataMember]
		[field: SerializeField, Tooltip(
			        "If true, this card bypasses normal pool selection and goes to the front of the priority queue automatically when its conditions are met.")]
		public bool ForcePriority { get; private set; }

		[DataMember]
		[field: SerializeField, Header("Appearance Conditions"),
		        Tooltip("All conditions must pass for this card to be eligible. Uses [SerializeReference] for polymorphism."), SerializeReference]
		public List<CardCondition> Conditions { get; private set; }
	}
}