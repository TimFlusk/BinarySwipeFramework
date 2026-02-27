using System;
using UnityEngine;
using VerdichotomyFramework.Cards;
namespace VerdichotomyFramework
{
    /// <summary>
    ///     Top-level configuration asset. One per game.
    ///     References all stats, pools, and the flag registry.
    ///     The GameStateManager and CardScheduler read from this at startup.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Verdichotomy/Game Config")]
	public class GameConfig : ScriptableObject
	{
		[Header("Stats"), Tooltip("All stats in the game, in the order they should appear in the UI.")]
		public StatDefinition[] stats = Array.Empty<StatDefinition>();

		[Header("Flags"), Tooltip("The single FlagRegistry asset for this game.")]
		public FlagRegistry flagRegistry;

		[Header("Card Pools"), Tooltip("All pools the scheduler will consider. Order doesn't affect selection.")]
		public CardPoolData[] pools = Array.Empty<CardPoolData>();

		[Header("Death & Restart"), Tooltip("How many seconds to show the death screen before restarting.")]
		public float deathScreenDuration = 3f;

		[Header("Debug"), Tooltip("If true, logs scheduler decisions to the console.")]
		public bool verboseSchedulerLogging;
	}
}