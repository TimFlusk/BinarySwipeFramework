using System;
using ReignsFramework.Runtime;
using UnityEngine;
namespace VerdichotomyFramework.Cards
{
	public enum SwipeDirection
	{
		Left,
		Right
	}

    /// <summary>
    ///     Mediates between the presentation layer (UI/input) and the data layer.
    ///     Responsibilities:
    ///     - Tracks the current active card and visit count
    ///     - Applies stat deltas, flag effects, and scheduling effects on commit
    ///     - Drives the turn cycle
    ///     The UI layer calls CommitSwipe() when the player finalises their choice.
    ///     It subscribes to the events below to know when to animate and update.
    /// </summary>
    public class CardPlayer : MonoBehaviour
	{
		// ── Dependencies ──────────────────────────────────────────────────────

		public GameStateManager stateManager;
		public CardScheduler scheduler;

		// ── State ─────────────────────────────────────────────────────────────

		private CardData _currentCard;
		private int _currentVisit;
		private bool _waitingForNextCard;

		// ── Lifecycle ─────────────────────────────────────────────────────────

		private void Start()
		{
			stateManager.OnRunStarted += OnRunStarted;
			stateManager.OnRunEnded += OnRunEnded;
			DealNextCard();
		}

		private void OnDestroy()
		{
			if (stateManager != null)
			{
				stateManager.OnRunStarted -= OnRunStarted;
				stateManager.OnRunEnded -= OnRunEnded;
			}
		}

		// ── Events ────────────────────────────────────────────────────────────

		/// <summary>Fired when a new card is ready for presentation.</summary>
		public event Action<CardData, int /* visitCount */> OnCardDealt;

		/// <summary>Fired during swipe gesture with direction and normalised progress (0‥1).</summary>
		public event Action<SwipeDirection, float> OnSwipeProgress;

		/// <summary>Fired when the player commits to a swipe, before effects apply.</summary>
		public event Action<SwipeDirection, OutcomeData> OnOutcomeCommitting;

		/// <summary>Fired after all effects have been applied.</summary>
		public event Action<SwipeDirection, OutcomeData> OnOutcomeApplied;

		private void OnRunStarted()
		{
			DealNextCard();
		}
		private void OnRunEnded()
		{
			_waitingForNextCard = true;
			// block dealing until new run
		}

		// ── Public API (called by UI layer) ───────────────────────────────────

        /// <summary>
        ///     Called by the UI during a swipe gesture to update preview state.
        ///     direction: which way the card is moving.
        ///     progress: 0 = centre, 1 = fully committed.
        /// </summary>
        public void UpdateSwipeProgress(SwipeDirection direction, float progress)
		{
			OnSwipeProgress?.Invoke(direction, progress);
		}

        /// <summary>
        ///     Called by the UI when the player releases and commits to a swipe.
        ///     This is the main entry point for resolving a card.
        /// </summary>
        public void CommitSwipe(SwipeDirection direction)
		{
			if (_currentCard == null) return;

			var outcome = direction == SwipeDirection.Left
				? _currentCard.GetLeftOutcomeForVisit(_currentVisit)
				: _currentCard.GetRightOutcomeForVisit(_currentVisit);

			OnOutcomeCommitting?.Invoke(direction, outcome);

			ApplyOutcome(outcome);

			OnOutcomeApplied?.Invoke(direction, outcome);

			// If a lethal stat was hit, OnRunEnded will have fired; don't deal next card yet.
			if (!_waitingForNextCard)
			{
				stateManager.AdvanceTurn();
				DealNextCard();
			}
		}

		// ── Internal ──────────────────────────────────────────────────────────

		private void DealNextCard()
		{
			_currentCard = scheduler.GetNextCard();
			_currentVisit = _currentCard != null
				? stateManager.GetVisitCount(_currentCard.cardId)
				: 0;

			// Visit count was just incremented by RecordCardShown; subtract 1 for 0-indexing.
			// (RecordCardShown increments before we read it here.)
			if (_currentVisit > 0) _currentVisit -= 1;

			if (_currentCard != null)
				OnCardDealt?.Invoke(_currentCard, _currentVisit);
		}

		private void ApplyOutcome(OutcomeData outcome)
		{
			if (outcome == null) return;

			// Stat deltas
			var deltas = outcome.ResolveDeltas(stateManager);
			foreach (var delta in deltas)
			{
				if (delta.stat == null) continue;
				var lethal = stateManager.ApplyStatDelta(delta.stat.statId, delta.delta);
				if (lethal)
				{
					_waitingForNextCard = true;
					return; // stop processing; run has ended
				}
			}

			// Flag effects
			foreach (var flagEffect in outcome.flagEffects)
			{
				stateManager.ApplyFlagEffect(flagEffect);
			}

			// Scheduling effects
			scheduler.ApplySchedulingEffects(outcome.schedulingEffects);
		}
	}
}