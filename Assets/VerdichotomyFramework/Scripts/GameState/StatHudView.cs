using System;
using UnityEngine;
namespace VerdichotomyFramework.GameState
{
	/// <summary>
	/// Manages all stat bars for the HUD. Subscribes to GameStateManager events.
	/// Create one StatBarView per stat and register them here.
	/// </summary>
	public class StatHudView : MonoBehaviour
	{
		[Header("Runtime")]
		public GameStateManager stateManager;

		[Header("Stat Bars"), Tooltip("One entry per StatDefinition. Must match the statId on each StatBarView.")]
		public StatBarView[] statBars = Array.Empty<StatBarView>();

		private void OnEnable()
		{
			if (stateManager == null)
			{
				return;
			}
			stateManager.OnStatChanged += HandleStatChanged;
			stateManager.OnRunStarted += HandleRunStarted;
		}

		private void OnDisable()
		{
			if (stateManager == null)
			{
				return;
			}
			stateManager.OnStatChanged -= HandleStatChanged;
			stateManager.OnRunStarted -= HandleRunStarted;
		}

		private void HandleStatChanged(string statId, int oldValue, int newValue)
		{
			foreach (var bar in statBars)
			{
				if (bar.Stat != null && bar.Stat.statId == statId)
				{
					bar.SetValue(newValue);
				}
					
			}
		}

		private void HandleRunStarted()
		{
			foreach (var bar in statBars)
			{
				if (bar.Stat != null)
				{
					bar.SetValue(bar.Stat.startingValue, false);
				}
			}
		}
	}
}