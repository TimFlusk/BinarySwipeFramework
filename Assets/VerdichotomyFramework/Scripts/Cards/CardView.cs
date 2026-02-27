using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VerdichotomyFramework.Cards.Data;
namespace VerdichotomyFramework.Cards
{
    /// <summary>
    ///     Drives the visual representation of a card.
    ///     Attach to the card GameObject in your UI hierarchy.
    ///     Wire up the serialized fields to your UI elements.
    ///     This component subscribes to CardPlayer events and updates itself.
    ///     Override the virtual methods to customise animation behaviour
    ///     without modifying this base class.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
	public class CardView : MonoBehaviour
	{
		// ── Dependencies ──────────────────────────────────────────────────────

		[Header("Runtime")]
		public CardPlayer cardPlayer;

		// ── UI References ─────────────────────────────────────────────────────

		[Header("Card Elements"), Tooltip("The card's root RectTransform (usually 'this').")]
		public RectTransform cardRect;

		[Tooltip("Portrait image — displays CharacterData.portrait.")]
		public Image portraitImage;

		[Tooltip("Optional animator for animated portraits.")]
		public Animator portraitAnimator;

		[Tooltip("Background image — displays CharacterData.backgroundSprite.")]
		public Image backgroundImage;

		[Tooltip("Prompt text label.")]
		public TMP_Text promptText;

		[Tooltip("Character name label.")]
		public TMP_Text characterNameText;

		[Header("Swipe Feedback"), Tooltip("Shown when swiping left; fades in with swipe progress.")]
		public CanvasGroup leftHintGroup;

		[Tooltip("Text inside leftHintGroup — shows outcome.swipeHintText.")]
		public TMP_Text leftHintText;

		[Tooltip("Shown when swiping right.")]
		public CanvasGroup rightHintGroup;

		[Tooltip("Text inside rightHintGroup.")]
		public TMP_Text rightHintText;

		[Header("Swipe Animation"), Tooltip("Maximum horizontal displacement of the card during a swipe gesture.")]
		public float maxSwipeOffset = 300f;

		[Tooltip("Maximum rotation of the card during a swipe gesture (degrees).")]
		public float maxSwipeRotation = 20f;

		[Tooltip("Duration of the card-leave animation when a swipe is committed (seconds).")]
		public float exitDuration = 0.3f;

		[Tooltip("Duration of the card-enter animation when a new card is dealt (seconds).")]
		public float enterDuration = 0.25f;

		// ── Audio ─────────────────────────────────────────────────────────────

		[Header("Audio")]
		public AudioSource audioSource;
		private Coroutine _animationCoroutine;
		private Vector2 _cardRestPosition;

		// ── State ─────────────────────────────────────────────────────────────

		private CardData _currentCard;
		private int _currentVisit;

		// ── Lifecycle ─────────────────────────────────────────────────────────

		protected virtual void Awake()
		{
			if (cardRect == null) cardRect = GetComponent<RectTransform>();
			_cardRestPosition = cardRect.anchoredPosition;
		}

		protected virtual void OnEnable()
		{
			if (cardPlayer == null) return;
			cardPlayer.OnCardDealt += HandleCardDealt;
			cardPlayer.OnSwipeProgress += HandleSwipeProgress;
			cardPlayer.OnOutcomeCommitting += HandleOutcomeCommitting;
		}

		protected virtual void OnDisable()
		{
			if (cardPlayer == null) return;
			cardPlayer.OnCardDealt -= HandleCardDealt;
			cardPlayer.OnSwipeProgress -= HandleSwipeProgress;
			cardPlayer.OnOutcomeCommitting -= HandleOutcomeCommitting;
		}

		// ── Event handlers ────────────────────────────────────────────────────

		protected virtual void HandleCardDealt(CardData card, int visitCount)
		{
			_currentCard = card;
			_currentVisit = visitCount;
			PopulateCard(card, visitCount);
			PlayEnterAnimation();
		}

		protected virtual void HandleSwipeProgress(SwipeDirection dir, float progress)
		{
			var offset = progress * maxSwipeOffset * (dir == SwipeDirection.Left ? -1f : 1f);
			var rotation = progress * maxSwipeRotation * (dir == SwipeDirection.Left ? 1f : -1f);

			cardRect.anchoredPosition = _cardRestPosition + new Vector2(offset, 0f);
			cardRect.localRotation = Quaternion.Euler(0f, 0f, rotation);

			if (leftHintGroup != null)
				leftHintGroup.alpha = dir == SwipeDirection.Left ? progress : 0f;
			if (rightHintGroup != null)
				rightHintGroup.alpha = dir == SwipeDirection.Right ? progress : 0f;
		}

		protected virtual void HandleOutcomeCommitting(SwipeDirection dir, OutcomeData outcome)
		{
			PlayExitAnimation(dir);
		}

		// ── Card population ───────────────────────────────────────────────────

		protected virtual void PopulateCard(CardData card, int visitCount)
		{
			// Text
			if (promptText != null)
				promptText.text = card.GetPromptForVisit(visitCount);

			if (characterNameText != null)
				characterNameText.text = card.character != null ? card.character.characterName : string.Empty;

			// Portrait
			if (portraitImage != null && card.character != null)
			{
				portraitImage.sprite = card.character.portrait;
				portraitImage.enabled = card.character.portrait != null;

				// Framing
				portraitImage.rectTransform.anchoredPosition = card.character.portraitOffset;
				portraitImage.rectTransform.localScale = Vector3.one * card.character.portraitScale;
			}

			// Animated portrait
			if (portraitAnimator != null && card.character != null)
			{
				if (card.character.animatorController != null)
				{
					portraitAnimator.runtimeAnimatorController = card.character.animatorController;
					portraitAnimator.enabled = true;
				}
				else
				{
					portraitAnimator.enabled = false;
				}
			}

			// Background
			if (backgroundImage != null && card.character != null)
			{
				backgroundImage.sprite = card.character.backgroundSprite;
				backgroundImage.enabled = card.character.backgroundSprite != null;
			}

			// Swipe hints
			var leftOutcome = card.GetLeftOutcomeForVisit(visitCount);
			var rightOutcome = card.GetRightOutcomeForVisit(visitCount);

			if (leftHintText != null) leftHintText.text = leftOutcome?.swipeHintText ?? string.Empty;
			if (rightHintText != null) rightHintText.text = rightOutcome?.swipeHintText ?? string.Empty;

			// Reset hint alphas
			if (leftHintGroup != null) leftHintGroup.alpha = 0f;
			if (rightHintGroup != null) rightHintGroup.alpha = 0f;

			// Audio
			if (audioSource != null && card.character != null)
			{
				if (card.character.ambientClip != null)
				{
					audioSource.clip = card.character.ambientClip;
					audioSource.loop = true;
					audioSource.Play();
				}
				else
				{
					audioSource.Stop();
				}

				if (card.character.dealSounds.Length > 0)
				{
					var clip = card.character.dealSounds[Random.Range(0, card.character.dealSounds.Length)];
					audioSource.PlayOneShot(clip);
				}
			}
		}

		// ── Animation ─────────────────────────────────────────────────────────

		protected virtual void PlayEnterAnimation()
		{
			if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
			_animationCoroutine = StartCoroutine(EnterRoutine());
		}

		protected virtual void PlayExitAnimation(SwipeDirection dir)
		{
			if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
			_animationCoroutine = StartCoroutine(ExitRoutine(dir));
		}

		private IEnumerator EnterRoutine()
		{
			// Slide in from a slight upward position, fade in
			var startPos = _cardRestPosition + new Vector2(0f, 60f);
			cardRect.anchoredPosition = startPos;
			cardRect.localRotation = Quaternion.identity;

			var elapsed = 0f;
			while (elapsed < enterDuration)
			{
				elapsed += Time.deltaTime;
				var t = Mathf.SmoothStep(0f, 1f, elapsed / enterDuration);
				cardRect.anchoredPosition = Vector2.Lerp(startPos, _cardRestPosition, t);
				yield return null;
			}
			cardRect.anchoredPosition = _cardRestPosition;
		}

		private IEnumerator ExitRoutine(SwipeDirection dir)
		{
			var targetX = dir == SwipeDirection.Left ? -maxSwipeOffset * 2f : maxSwipeOffset * 2f;
			var targetRot = dir == SwipeDirection.Left ? maxSwipeRotation : -maxSwipeRotation;
			var startPos = cardRect.anchoredPosition;
			var endPos = _cardRestPosition + new Vector2(targetX, 0f);
			var startRot = cardRect.localRotation;
			var endRot = Quaternion.Euler(0f, 0f, targetRot);

			var elapsed = 0f;
			while (elapsed < exitDuration)
			{
				elapsed += Time.deltaTime;
				var t = elapsed / exitDuration;
				cardRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
				cardRect.localRotation = Quaternion.Lerp(startRot, endRot, t);
				yield return null;
			}

			// Reset silently for reuse
			cardRect.anchoredPosition = _cardRestPosition;
			cardRect.localRotation = Quaternion.identity;
		}
	}
}