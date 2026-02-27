using System;
using System.Collections.Generic;
using UnityEngine;
using VerdichotomyFramework.Cards.Data;
using Random = UnityEngine.Random;
namespace VerdichotomyFramework.Cards
{
    /// <summary>
    ///     Decides which card to show each turn.
    ///     Priority order:
    ///     1. ForceNext card (set by a previous outcome's scheduling effect)
    ///     2. Priority queue (cards queued via QueueWithPriority effect)
    ///     3. ForcePriority cards from eligible pools (conditions met + cooldown expired)
    ///     4. Normal weighted-random draw from eligible pools
    ///     The scheduler never delivers a card it cannot resolve; if the eligible
    ///     set is empty, it returns null and logs a warning.
    /// </summary>
    public class CardScheduler : MonoBehaviour
	{
		// ── Dependencies ──────────────────────────────────────────────────────

		[Tooltip("Reference to the game's state manager.")]
		public GameStateManager stateManager;

		[Tooltip("The game configuration asset (same one as GameStateManager).")]
		public GameConfig config;
		private readonly HashSet<string> _disabledPools = new();

		// ── Internal queues ───────────────────────────────────────────────────

		private CardData _forcedNext;
		private readonly Queue<CardData> _priorityQueue = new();
		private readonly HashSet<string> _removedCards = new(); // cardId → removed this run

		// ── Pool enabled/disabled state ───────────────────────────────────────

		private void Awake()
		{
			// Seed disabled pools from config defaults
			foreach (var pool in config.pools)
			{
				if (!pool.enabledByDefault)
					_disabledPools.Add(pool.poolId);
			}

			stateManager.OnRunStarted += OnRunStarted;
		}

		private void OnDestroy()
		{
			if (stateManager != null)
				stateManager.OnRunStarted -= OnRunStarted;
		}

		// ── Events ────────────────────────────────────────────────────────────

		public event Action<CardData> OnCardScheduled;

		private void OnRunStarted()
		{
			_forcedNext = null;
			_priorityQueue.Clear();
			_removedCards.Clear();

			_disabledPools.Clear();
			foreach (var pool in config.pools)
			{
				if (!pool.enabledByDefault)
					_disabledPools.Add(pool.poolId);
			}
		}

		// ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        ///     Returns the next card to show, advancing the scheduler's internal state.
        ///     Call this once per turn before presenting the card to the player.
        /// </summary>
        public CardData GetNextCard()
		{
			CardData card = null;

			// 1. Hard forced next
			if (_forcedNext != null)
			{
				card = _forcedNext;
				_forcedNext = null;
				Log($"[Scheduler] Forced next: {card.cardId}");
			}

			// 2. Priority queue
			else if (_priorityQueue.Count > 0)
			{
				card = _priorityQueue.Dequeue();
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
        ///     Apply all scheduling effects from a chosen outcome.
        ///     Called by CardPlayer after the player commits to a swipe.
        /// </summary>
        public void ApplySchedulingEffects(CardSchedulingEffect[] effects)
		{
			foreach (var effect in effects)
			{
				switch (effect.effectType)
				{
					case CardEffectType.ForceNext:
						_forcedNext = effect.targetCard;
						Log($"[Scheduler] ForceNext set: {effect.targetCard?.cardId}");
						break;

					case CardEffectType.QueueWithPriority:
						if (effect.targetCard != null)
						{
							_priorityQueue.Enqueue(effect.targetCard);
							Log($"[Scheduler] Queued: {effect.targetCard.cardId}");
						}
						break;

					case CardEffectType.EnablePool:
						if (effect.targetPool != null)
						{
							_disabledPools.Remove(effect.targetPool.poolId);
							Log($"[Scheduler] Pool enabled: {effect.targetPool.poolId}");
						}
						break;

					case CardEffectType.DisablePool:
						if (effect.targetPool != null)
						{
							_disabledPools.Add(effect.targetPool.poolId);
							Log($"[Scheduler] Pool disabled: {effect.targetPool.poolId}");
						}
						break;

					case CardEffectType.RemoveCardFromRun:
						if (effect.targetCard != null)
						{
							_removedCards.Add(effect.targetCard.cardId);
							Log($"[Scheduler] Card removed from run: {effect.targetCard.cardId}");
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

			foreach (var pool in config.pools)
			{
				if (_disabledPools.Contains(pool.poolId)) continue;
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
					if (card.scheduling.forcePriority)
					{
						Log($"[Scheduler] ForcePriority card drawn: {card.cardId}");
						return card;
					}
				}
			}

			// Weighted pool selection
			var selectedPool = WeightedRandom(eligiblePools, p => p.pool.poolWeight);
			Log($"[Scheduler] Pool selected: {selectedPool.pool.poolId}");

			// Weighted card selection within pool
			var selectedCard = WeightedRandom(selectedPool.cards, c => c.scheduling.weight);
			Log($"[Scheduler] Card selected: {selectedCard.cardId}");

			return selectedCard;
		}

		private List<CardData> GetEligibleCards(CardPoolData pool)
		{
			var result = new List<CardData>();

			foreach (var card in pool.cards)
			{
				if (card == null) continue;
				if (_removedCards.Contains(card.cardId)) continue;

				// OneShot: skip if already seen
				if (card.scheduling.recurrence == CardRecurrence.OneShot &&
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