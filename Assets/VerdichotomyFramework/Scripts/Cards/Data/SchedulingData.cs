using System;
using System.Collections.Generic;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Data
{
	// ─────────────────────────────────────────────────────────────────────────
	// Scheduling settings embedded in CardData
	// ─────────────────────────────────────────────────────────────────────────

	[Serializable]
	public class SchedulingData
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
		public List<CardCondition> conditions;
	}
}