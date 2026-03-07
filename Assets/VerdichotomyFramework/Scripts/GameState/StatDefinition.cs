using UnityEngine;
namespace VerdichotomyFramework.GameState
{
    /// <summary>
    ///     Defines a single stat in the game (e.g. Church, People, Army, Treasury).
    ///     Designers create one of these per stat. The framework reads them at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStat", menuName = "Reigns/Stat Definition")]
	public class StatDefinition : ScriptableObject
	{
		[Header("Identity"), Tooltip("Unique ID used in code and save data. Never change after shipping.")]
		public string statId;

		[Tooltip("Display name shown in UI.")]
		public string displayName;

		[Tooltip("Icon shown in the stat bar.")]
		public Sprite icon;

		[Tooltip("Colour used to tint the stat bar.")]
		public Color barColour = Color.white;

		[Header("Range"), Tooltip("Starting value for a new run."), Range(0, 100)]
		public int startingValue = 50;

		[Tooltip("Inclusive minimum. Reaching this triggers death if killAtMin is true.")]
		public int minValue;

		[Tooltip("Inclusive maximum. Reaching this triggers death if killAtMax is true.")]
		public int maxValue = 100;

		[Header("Death Conditions")]
		public bool killAtMin = true;
		public bool killAtMax = true;

		[Tooltip("Message shown when this stat causes death at minimum."), TextArea]
		public string deathMessageMin = "Your reign has ended.";

		[Tooltip("Message shown when this stat causes death at maximum."), TextArea]
		public string deathMessageMax = "Your reign has ended.";

		// ── Helpers ──────────────────────────────────────────────────────────

		public bool IsLethal(int value)
		{
			if (killAtMin && value <= minValue) return true;
			if (killAtMax && value >= maxValue) return true;
			return false;
		}

		public int Clamp(int value)
		{
			return Mathf.Clamp(value, minValue, maxValue);
		}

		/// <summary>Normalised 0‥1 value for UI bars.</summary>
		public float Normalise(int value)
		{
			return Mathf.InverseLerp(minValue, maxValue, value);
		}
	}
}