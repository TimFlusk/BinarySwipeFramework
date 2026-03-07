using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VerdichotomyFramework.Cards.Data;
using VerdichotomyFramework.GameState;
namespace VerdichotomyFramework.Cards
{
    /// <summary>
    /// Drives the visual representation of a card.
    /// Attach to the card GameObject in your UI hierarchy.
    /// Wire up the serialized fields to your UI elements.
    /// This component subscribes to CardPlayer events and updates itself.
    /// Override the virtual methods to customise animation behaviour
    /// without modifying this base class.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
	public class CardView : MonoBehaviour
	{
		// ── Dependencies ──────────────────────────────────────────────────────

		[SerializeField, Header("Runtime")]
		private CardPlayer cardPlayer;

		// ── UI References ─────────────────────────────────────────────────────

		[SerializeField, Header("Card Elements"), Tooltip("The card's root RectTransform (usually 'this').")]
		private RectTransform cardRect;

		[SerializeField, Tooltip("Portrait image — displays CharacterData.portrait.")]
		private Image portraitImage;

		[SerializeField, Tooltip("Optional animator for animated portraits.")]
		private Animator portraitAnimator;

		[SerializeField, Tooltip("Background image — displays CharacterData.backgroundSprite.")]
		private Image backgroundImage;

		[SerializeField, Tooltip("Prompt text label.")]
		private TMP_Text promptText;

		[SerializeField, Tooltip("Character name label.")]
		private TMP_Text characterNameText;

		[SerializeField, Header("Swipe Feedback"), Tooltip("Shown when swiping left; fades in with swipe progress.")]
		private CanvasGroup leftHintGroup;

		[SerializeField, Tooltip("Text inside leftHintGroup — shows outcome.swipeHintText.")]
		private TMP_Text leftHintText;

		[SerializeField, Tooltip("Shown when swiping right.")]
		private CanvasGroup rightHintGroup;

		[SerializeField, Tooltip("Text inside rightHintGroup.")]
		private TMP_Text rightHintText;

		[SerializeField, Header("Swipe Animation"), Tooltip("Maximum horizontal displacement of the card during a swipe gesture.")]
		private float maxSwipeOffset = 300f;

		[SerializeField, Tooltip("Maximum rotation of the card during a swipe gesture (degrees).")]
		private float maxSwipeRotation = 20f;

		[SerializeField, Tooltip("Duration of the card-leave animation when a swipe is committed (seconds).")]
		private float exitDuration = 0.3f;

		[SerializeField, Tooltip("Duration of the card-enter animation when a new card is dealt (seconds).")]
		private float enterDuration = 0.25f;

		// ── Audio ─────────────────────────────────────────────────────────────

		[Header("Audio")]
		public AudioSource audioSource;
		
		// TODO: No. Should be undo as quickly as possible. Who even make a coroutine a field?
		private Coroutine animationCoroutine;
		private Vector2 cardRestPosition;

		// ── State ─────────────────────────────────────────────────────────────

		private CardData currentCard;
		private int currentVisit;

		// ── Lifecycle ─────────────────────────────────────────────────────────

		protected virtual void Awake()
		{
			if (cardRect == null)
			{
				cardRect = GetComponent<RectTransform>();
			}
			cardRestPosition = cardRect.anchoredPosition;
		}

		protected virtual void OnEnable()
		{
			if (cardPlayer == null)
			{
				return;
			}
			cardPlayer.OnCardDealt += HandleCardDealt;
			cardPlayer.OnSwipeProgress += HandleSwipeProgress;
			cardPlayer.OnOutcomeCommitting += HandleOutcomeCommitting;
		}

		protected virtual void OnDisable()
		{
			if (cardPlayer == null)
			{
				return;
			}
			cardPlayer.OnCardDealt -= HandleCardDealt;
			cardPlayer.OnSwipeProgress -= HandleSwipeProgress;
			cardPlayer.OnOutcomeCommitting -= HandleOutcomeCommitting;
		}

		// ── Event handlers ────────────────────────────────────────────────────

		protected virtual void HandleCardDealt(CardData card, int visitCount)
		{
			currentCard = card;
			currentVisit = visitCount;
			PopulateCard(card, visitCount);
			PlayEnterAnimation();
		}

		protected virtual void HandleSwipeProgress(SwipeDirection dir, float progress)
		{
			var offset = progress * maxSwipeOffset * (dir == SwipeDirection.Left ? -1f : 1f);
			var rotation = progress * maxSwipeRotation * (dir == SwipeDirection.Left ? 1f : -1f);

			cardRect.anchoredPosition = cardRestPosition + new Vector2(offset, 0f);
			cardRect.localRotation = Quaternion.Euler(0f, 0f, rotation);

			if (leftHintGroup != null)
			{
				leftHintGroup.alpha = dir == SwipeDirection.Left ? progress : 0f;
			}

			if (rightHintGroup != null)
			{
				rightHintGroup.alpha = dir == SwipeDirection.Right ? progress : 0f;
			}
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

			if (leftHintText != null) leftHintText.text = leftOutcome?.SwipeHintText ?? string.Empty;
			if (rightHintText != null) rightHintText.text = rightOutcome?.SwipeHintText ?? string.Empty;

			// Reset hint alphas
			if (leftHintGroup != null)
			{
				leftHintGroup.alpha = 0f;
			}
			if (rightHintGroup != null)
			{
				rightHintGroup.alpha = 0f;
			}

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
			if (animationCoroutine != null) StopCoroutine(animationCoroutine);
			animationCoroutine = StartCoroutine(EnterRoutine());
		}

		protected virtual void PlayExitAnimation(SwipeDirection dir)
		{
			if (animationCoroutine != null) StopCoroutine(animationCoroutine);
			animationCoroutine = StartCoroutine(ExitRoutine(dir));
		}

		private IEnumerator EnterRoutine()
		{
			// Slide in from a slight upward position, fade in
			var startPos = cardRestPosition + new Vector2(0f, 60f);
			cardRect.anchoredPosition = startPos;
			cardRect.localRotation = Quaternion.identity;

			var elapsed = 0f;
			while (elapsed < enterDuration)
			{
				elapsed += Time.deltaTime;
				var t = Mathf.SmoothStep(0f, 1f, elapsed / enterDuration);
				cardRect.anchoredPosition = Vector2.Lerp(startPos, cardRestPosition, t);
				yield return null;
			}
			cardRect.anchoredPosition = cardRestPosition;
		}

		private IEnumerator ExitRoutine(SwipeDirection dir)
		{
			var targetX = dir == SwipeDirection.Left ? -maxSwipeOffset * 2f : maxSwipeOffset * 2f;
			var targetRot = dir == SwipeDirection.Left ? maxSwipeRotation : -maxSwipeRotation;
			var startPos = cardRect.anchoredPosition;
			var endPos = cardRestPosition + new Vector2(targetX, 0f);
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
			cardRect.anchoredPosition = cardRestPosition;
			cardRect.localRotation = Quaternion.identity;
		}
	}
}