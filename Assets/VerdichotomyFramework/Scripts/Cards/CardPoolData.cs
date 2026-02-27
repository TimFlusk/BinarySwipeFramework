using System;
using System.Collections.Generic;
using ReignsFramework.Runtime;
using UnityEngine;
namespace VerdichotomyFramework.Cards
{
    /// <summary>
    ///     A named, weighted collection of cards.
    ///     The CardScheduler evaluates pools before evaluating individual cards.
    ///     Pools can be enabled/disabled at runtime via CardSchedulingEffects.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardPool", menuName = "Reigns/Card Pool")]
	public class CardPoolData : ScriptableObject
	{
		[Header("Identity"), Tooltip("Unique ID. Used in save data and scheduling effects.")]
		public string poolId;

		[Header("Cards"), Tooltip("All cards belonging to this pool.")]
		public List<CardData> cards = new();

		[Header("Pool Conditions"), Tooltip("All conditions must pass for ANY card in this pool to be eligible. " +
		                                    "Individual card conditions are checked on top of these."), SerializeReference]
		public CardCondition[] poolConditions = Array.Empty<CardCondition>();

		[Header("Pool Settings"), Tooltip("Relative weight of this pool vs other active pools when the scheduler picks a pool to draw from."),
		 Range(1, 100)]
		public int poolWeight = 10;

		[Tooltip("If false, this pool is ignored by the scheduler (can be toggled at runtime).")]
		public bool enabledByDefault = true;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrEmpty(poolId))
				poolId = name;
		}
#endif

		// ── Helpers ───────────────────────────────────────────────────────────

		public bool ArePoolConditionsMet(GameStateManager state)
		{
			foreach (var c in poolConditions)
			{
				if (c != null && !c.Evaluate(state)) return false;
			}
			return true;
		}
	}
}