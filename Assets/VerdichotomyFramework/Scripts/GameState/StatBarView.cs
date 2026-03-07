using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace VerdichotomyFramework.GameState
{
    /// <summary>
    /// Represents a single stat bar in the HUD.
    /// Create one of these per StatDefinition and wire them into StatHudView.
    /// </summary>
    public class StatBarView : MonoBehaviour
	{
		[field: SerializeField, Header("Identity"), Tooltip("Which stat this bar represents.")]
		public StatDefinition Stat { get; private set; }

		[SerializeField, Header("UI References")]
		private Image fillImage;
		
		[SerializeField]
		private Image iconImage;
		
		[SerializeField]
		private TMP_Text valueLabel; // optional numeric label

		[SerializeField, Header("Animation"), Tooltip("Speed at which the bar fill animates toward the target value.")]
		private float lerpSpeed = 5f;

		[SerializeField, Tooltip("Scale pulse played when the stat changes.")]
		private float pulseMagnitude = 1.2f;
		
		[SerializeField]
		private float pulseDuration = 0.15f;
		
		private float currentFill;
		
		private float pulseTimer;

		// ── State ─────────────────────────────────────────────────────────────

		private float targetFill;

		// ── Lifecycle ─────────────────────────────────────────────────────────

		private void Awake()
		{
			if (Stat == null)
			{
				return;
			}
			if (fillImage != null)
			{
				fillImage.color = Stat.barColour;
			}
			if (iconImage != null)
			{
				iconImage.sprite = Stat.icon;
			}

			targetFill = Stat.Normalise(Stat.startingValue);
			currentFill = targetFill;
			ApplyFill(currentFill);
		}

		private void Update()
		{
			// Smooth fill
			if (!Mathf.Approximately(currentFill, targetFill))
			{
				currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * lerpSpeed);
				ApplyFill(currentFill);
			}

			// Pulse
			if (pulseTimer > 0f)
			{
				pulseTimer -= Time.deltaTime;
				var t = pulseTimer / pulseDuration;
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
			if (Stat == null) return;
			targetFill = Stat.Normalise(value);

			if (!animate)
			{
				currentFill = targetFill;
				ApplyFill(currentFill);
			}

			if (valueLabel != null)
				valueLabel.text = value.ToString();

			pulseTimer = pulseDuration;
		}

		// ── Private ───────────────────────────────────────────────────────────

		private void ApplyFill(float normalised)
		{
			if (fillImage != null) fillImage.fillAmount = normalised;
		}
	}

	// ─────────────────────────────────────────────────────────────────────────

    
}