using System;
using System.Collections.Generic;
using UnityEngine;
using VerdichotomyFramework.Cards.Data;
using VerdichotomyFramework.GameState;
using Random = UnityEngine.Random;
namespace VerdichotomyFramework.Cards
{
    /// <summary>
    /// Decides which card to show each turn.
    /// Priority order:
    /// <list type="number">
    /// <item>ForceNext card (set by a previous outcome's scheduling effect).</item>
    /// <item>Priority queue (cards queued via QueueWithPriority effect).</item>
    /// <item>ForcePriority cards from eligible pools (conditions met + cooldown expired).</item>
    /// <item>Normal weighted-random draw from eligible pools.</item>
    /// </list>
    /// The scheduler never delivers a card it cannot resolve; if the eligible
    /// set is empty, it returns null and logs a warning.
    /// </summary>
    public class CardScheduler : MonoBehaviour
	{
		// ── Dependencies ──────────────────────────────────────────────────────

		[SerializeField, Tooltip("Reference to the game's state manager.")]
		private GameStateManager stateManager;

		[SerializeField, Tooltip("The game configuration asset (same one as GameStateManager).")]
		private GameConfig config;
		
		private readonly HashSet<string> disabledPools = new();

		// ── Internal queues ───────────────────────────────────────────────────

		private CardData forcedNext;
		private readonly Queue<CardData> priorityQueue = new();
		private readonly HashSet<string> removedCards = new(); // cardId → removed this run

		// ── Events ────────────────────────────────────────────────────────────
		public event Action<CardData> OnCardScheduled;
		
		
		// ── Pool enabled/disabled state ───────────────────────────────────────

		private void Awake()
		{
			// Seed disabled pools from config defaults
			foreach (var pool in config.Pools)
			{
				if (!pool.EnabledByDefault)
					disabledPools.Add(pool.PoolId);
			}

			stateManager.OnRunStarted += OnRunStarted;
		}

		private void OnDestroy()
		{
			if (stateManager != null)
				stateManager.OnRunStarted -= OnRunStarted;
		}

		// ── Events ────────────────────────────────────────────────────────────

		private void OnRunStarted()
		{
			forcedNext = null;
			priorityQueue.Clear();
			removedCards.Clear();

			disabledPools.Clear();
			foreach (var pool in config.Pools)
			{
				if (!pool.EnabledByDefault)
					disabledPools.Add(pool.PoolId);
			}
		}

		// ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the next card to show, advancing the scheduler's internal state.
        /// Call this once per turn before presenting the card to the player.
        /// </summary>
        public CardData GetNextCard()
		{
			CardData card = null;

			// 1. Hard forced next
			if (forcedNext != null)
			{
				card = forcedNext;
				forcedNext = null;
				Log($"[Scheduler] Forced next: {card.cardId}");
			}

			// 2. Priority queue
			else if (priorityQueue.Count > 0)
			{
				card = priorityQueue.Dequeue();
				Log($"[Scheduler] From priority queue: {card.cardId}");
			}

			// 3 & 4. Pool draw
			else
			{
				card = DrawFromPools();
			}

			if (card != null)
			{
				stateManager.RecordCardShown(card.cardId);
				OnCardScheduled?.Invoke(card);
			}
			else
			{
				Debug.LogWarning("[CardScheduler] No eligible card found. Check pool conditions and card conditions.");
			}

			return card;
		}

        /// <summary>
        /// Apply all scheduling effects from a chosen outcome.
        /// Called by CardPlayer after the player commits to a swipe.
        /// </summary>
        public void ApplySchedulingEffects(CardSchedulingEffect[] effects)
		{
			foreach (var effect in effects)
			{
				switch (effect.EffectType)
				{
					case CardEffectType.ForceNext:
						forcedNext = effect.TargetCard;
						Log($"[Scheduler] ForceNext set: {effect.TargetCard?.cardId}");
						break;

					case CardEffectType.QueueWithPriority:
						if (effect.TargetCard != null)
						{
							priorityQueue.Enqueue(effect.TargetCard);
							Log($"[Scheduler] Queued: {effect.TargetCard.cardId}");
						}
						break;

					case CardEffectType.EnablePool:
						if (effect.TargetPool != null)
						{
							disabledPools.Remove(effect.TargetPool.PoolId);
							Log($"[Scheduler] Pool enabled: {effect.TargetPool.PoolId}");
						}
						break;

					case CardEffectType.DisablePool:
						if (effect.TargetPool != null)
						{
							disabledPools.Add(effect.TargetPool.PoolId);
							Log($"[Scheduler] Pool disabled: {effect.TargetPool.PoolId}");
						}
						break;

					case CardEffectType.RemoveCardFromRun:
						if (effect.TargetCard != null)
						{
							removedCards.Add(effect.TargetCard.cardId);
							Log($"[Scheduler] Card removed from run: {effect.TargetCard.cardId}");
						}
						break;
				}
			}
		}

		// ── Pool draw logic ───────────────────────────────────────────────────

		private CardData DrawFromPools()
		{
			// Build list of (pool, eligibleCards) pairs
			var eligiblePools = new List<(CardPoolData pool, List<CardData> cards)>();

			foreach (var pool in config.Pools)
			{
				if (disabledPools.Contains(pool.PoolId)) continue;
				if (!pool.ArePoolConditionsMet(stateManager)) continue;

				var eligible = GetEligibleCards(pool);
				if (eligible.Count > 0)
					eligiblePools.Add((pool, eligible));
			}

			if (eligiblePools.Count == 0) return null;

			// Check for forcePriority cards first (across all eligible pools)
			foreach (var (_, cards) in eligiblePools)
			{
				foreach (var card in cards)
				{
					if (card.scheduling.ForcePriority)
					{
						Log($"[Scheduler] ForcePriority card drawn: {card.cardId}");
						return card;
					}
				}
			}

			// Weighted pool selection
			var selectedPool = WeightedRandom(eligiblePools, p => p.pool.PoolWeight);
			Log($"[Scheduler] Pool selected: {selectedPool.pool.PoolId}");

			// Weighted card selection within pool
			var selectedCard = WeightedRandom(selectedPool.cards, c => c.scheduling.Weight);
			Log($"[Scheduler] Card selected: {selectedCard.cardId}");

			return selectedCard;
		}

		private List<CardData> GetEligibleCards(CardPoolData pool)
		{
			var result = new List<CardData>();

			foreach (var card in pool.Cards)
			{
				if (card == null) continue;
				if (removedCards.Contains(card.cardId)) continue;

				// OneShot: skip if already seen
				if (card.scheduling.Recurrence == Recurrence.OneShot &&
				    stateManager.GetVisitCount(card.cardId) > 0) continue;

				// Cooldown
				if (!stateManager.IsCooldownExpired(card)) continue;

				// Card-level conditions
				if (!card.AreConditionsMet(stateManager)) continue;

				result.Add(card);
			}

			return result;
		}

		// ── Weighted random helper ────────────────────────────────────────────

		private T WeightedRandom<T>(List<T> items, Func<T, int> weightSelector)
		{
			var total = 0;
			foreach (var item in items)
			{
				total += weightSelector(item);
			}

			var roll = Random.Range(0, total);
			var cumulative = 0;

			foreach (var item in items)
			{
				cumulative += weightSelector(item);
				if (roll < cumulative) return item;
			}

			return items[items.Count - 1]; // fallback
		}

		// ── Logging ───────────────────────────────────────────────────────────

		private void Log(string msg)
		{
			if (config.verboseSchedulerLogging)
				Debug.Log(msg);
		}
	}
}