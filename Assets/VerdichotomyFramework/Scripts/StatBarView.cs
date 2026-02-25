using System;
using ReignsFramework.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace VerdichotomyFramework
{
    /// <summary>
    ///     Represents a single stat bar in the HUD.
    ///     Create one of these per StatDefinition and wire them into StatHudView.
    /// </summary>
    public class StatBarView : MonoBehaviour
	{
		[Header("Identity"), Tooltip("Which stat this bar represents.")]
		public StatDefinition stat;

		[Header("UI References")]
		public Image fillImage;
		public Image iconImage;
		public TMP_Text valueLabel; // optional numeric label

		[Header("Animation"), Tooltip("Speed at which the bar fill animates toward the target value.")]
		public float lerpSpeed = 5f;

		[Tooltip("Scale pulse played when the stat changes.")]
		public float pulseMagnitude = 1.2f;
		public float pulseDuration = 0.15f;
		private float _currentFill;
		private float _pulseTimer;

		// ── State ─────────────────────────────────────────────────────────────

		private float _targetFill;

		// ── Lifecycle ─────────────────────────────────────────────────────────

		private void Awake()
		{
			if (stat == null) return;
			if (fillImage != null) fillImage.color = stat.barColour;
			if (iconImage != null) iconImage.sprite = stat.icon;

			_targetFill = stat.Normalise(stat.startingValue);
			_currentFill = _targetFill;
			ApplyFill(_currentFill);
		}

		private void Update()
		{
			// Smooth fill
			if (!Mathf.Approximately(_currentFill, _targetFill))
			{
				_currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * lerpSpeed);
				ApplyFill(_currentFill);
			}

			// Pulse
			if (_pulseTimer > 0f)
			{
				_pulseTimer -= Time.deltaTime;
				var t = _pulseTimer / pulseDuration;
				var scale = Mathf.Lerp(1f, pulseMagnitude, t);
				transform.localScale = Vector3.one * scale;
			}
			else
			{
				transform.localScale = Vector3.one;
			}
		}

		// ── Public API ────────────────────────────────────────────────────────

		/// <summary>Update the bar to reflect a new stat value.</summary>
		public void SetValue(int value, bool animate = true)
		{
			if (stat == null) return;
			_targetFill = stat.Normalise(value);

			if (!animate)
			{
				_currentFill = _targetFill;
				ApplyFill(_currentFill);
			}

			if (valueLabel != null)
				valueLabel.text = value.ToString();

			_pulseTimer = pulseDuration;
		}

		// ── Private ───────────────────────────────────────────────────────────

		private void ApplyFill(float normalised)
		{
			if (fillImage != null) fillImage.fillAmount = normalised;
		}
	}

	// ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Manages all stat bars for the HUD. Subscribes to GameStateManager events.
    ///     Create one StatBarView per stat and register them here.
    /// </summary>
    public class StatHudView : MonoBehaviour
	{
		[Header("Runtime")]
		public GameStateManager stateManager;

		[Header("Stat Bars"), Tooltip("One entry per StatDefinition. Must match the statId on each StatBarView.")]
		public StatBarView[] statBars = Array.Empty<StatBarView>();

		private void OnEnable()
		{
			if (stateManager == null) return;
			stateManager.OnStatChanged += HandleStatChanged;
			stateManager.OnRunStarted += HandleRunStarted;
		}

		private void OnDisable()
		{
			if (stateManager == null) return;
			stateManager.OnStatChanged -= HandleStatChanged;
			stateManager.OnRunStarted -= HandleRunStarted;
		}

		private void HandleStatChanged(string statId, int oldValue, int newValue)
		{
			foreach (var bar in statBars)
			{
				if (bar.stat != null && bar.stat.statId == statId)
					bar.SetValue(newValue);
			}
		}

		private void HandleRunStarted()
		{
			foreach (var bar in statBars)
			{
				if (bar.stat != null)
					bar.SetValue(bar.stat.startingValue, false);
			}
		}
	}
}