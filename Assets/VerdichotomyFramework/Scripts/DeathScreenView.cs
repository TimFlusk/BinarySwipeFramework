using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace VerdichotomyFramework
{
    /// <summary>
    ///     Shows a death screen when a run ends, then triggers a new run.
    /// </summary>
    public class DeathScreenView : MonoBehaviour
	{
		// ── Dependencies ──────────────────────────────────────────────────────

		public GameStateManager stateManager;
		public GameConfig config;

		// ── UI References ─────────────────────────────────────────────────────

		[Header("UI")]
		public CanvasGroup deathScreenGroup;
		public TMP_Text deathMessageText;
		public TMP_Text reignLengthText;
		public Button restartButton;

		private void Start()
		{
			if (deathScreenGroup != null)
			{
				deathScreenGroup.alpha = 0f;
				deathScreenGroup.interactable = false;
				deathScreenGroup.blocksRaycasts = false;
			}

			if (restartButton != null)
				restartButton.onClick.AddListener(OnRestartPressed);
		}

		// ── Lifecycle ─────────────────────────────────────────────────────────

		private void OnEnable()
		{
			if (stateManager == null) return;
			stateManager.OnStatLethal += HandleStatLethal;
		}

		private void OnDisable()
		{
			if (stateManager == null) return;
			stateManager.OnStatLethal -= HandleStatLethal;
		}

		// ── Handlers ──────────────────────────────────────────────────────────

		private void HandleStatLethal(string statId)
		{
			// Find death message from stat definition
			var message = "Your reign has ended.";
			foreach (var stat in config.stats)
			{
				if (stat.statId == statId)
				{
					var val = stateManager.GetStat(statId);
					message = val <= stat.minValue ? stat.deathMessageMin : stat.deathMessageMax;
					break;
				}
			}

			if (deathMessageText != null)
				deathMessageText.text = message;

			if (reignLengthText != null)
				reignLengthText.text = $"You reigned for {stateManager.CurrentTurn} years.";

			StartCoroutine(ShowDeathScreen());
		}

		private void OnRestartPressed()
		{
			StartCoroutine(HideDeathScreen());
			stateManager.StartRun();
		}

		// ── Animation ─────────────────────────────────────────────────────────

		private IEnumerator ShowDeathScreen()
		{
			yield return new WaitForSeconds(0.5f);

			if (deathScreenGroup == null) yield break;

			deathScreenGroup.blocksRaycasts = true;
			var elapsed = 0f;
			var fadeDuration = 0.6f;
			while (elapsed < fadeDuration)
			{
				elapsed += Time.deltaTime;
				deathScreenGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
				yield return null;
			}
			deathScreenGroup.alpha = 1f;
			deathScreenGroup.interactable = true;

			// Auto-restart if no button assigned
			if (restartButton == null)
			{
				yield return new WaitForSeconds(config.deathScreenDuration);
				OnRestartPressed();
			}
		}

		private IEnumerator HideDeathScreen()
		{
			if (deathScreenGroup == null) yield break;

			deathScreenGroup.interactable = false;
			var elapsed = 0f;
			var fadeDuration = 0.4f;
			while (elapsed < fadeDuration)
			{
				elapsed += Time.deltaTime;
				deathScreenGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
				yield return null;
			}
			deathScreenGroup.alpha = 0f;
			deathScreenGroup.blocksRaycasts = false;
		}
	}
}