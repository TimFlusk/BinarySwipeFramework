using System;
using System.Collections.Generic;
using UnityEngine;
using VerdichotomyFramework.Cards.Condition;
using VerdichotomyFramework.Cards.Data;
using VerdichotomyFramework.GameState;
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
		[field: SerializeField]
		[Header("Identity"), Tooltip("Unique ID. Used in save data and scheduling effects.")]
		public string PoolId { get; 
			
#if !UNITY_EDITOR
			private set; 
#else
			set;
#endif
		}

		[field: SerializeField]
		[Header("Cards"), Tooltip("All cards belonging to this pool.")]
		public List<CardData> Cards { get; private set; } = new();

		[field: SerializeField]
		[Header("Pool Conditions"), Tooltip("All conditions must pass for ANY card in this pool to be eligible. " +
		                                    "Individual card conditions are checked on top of these.")]
		public CardCondition[] PoolConditions { get; private set; } = Array.Empty<CardCondition>();

		[field: SerializeField]
		[Header("Pool Settings"), Tooltip("Relative weight of this pool vs other active pools when the scheduler picks a pool to draw from."),
		 Range(1, 100)]
		public int PoolWeight { get; private set; } = 10;

		[Tooltip("If false, this pool is ignored by the scheduler (can be toggled at runtime).")]
		public bool EnabledByDefault = true;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrEmpty(PoolId))
				PoolId = name;
		}
#endif

		// ── Helpers ───────────────────────────────────────────────────────────

		public bool ArePoolConditionsMet(GameStateManager state)
		{
			foreach (var c in PoolConditions)
			{
				if (c != null && !c.Evaluate(state)) return false;
			}
			return true;
		}
	}
}