using System;
using UnityEngine;
using VerdichotomyFramework.Cards;
using VerdichotomyFramework.Cards.Flags;
namespace VerdichotomyFramework.GameState
{
    /// <summary>
    ///     Top-level configuration asset. One per game.
    ///     References all stats, pools, and the flag registry.
    ///     The GameStateManager and CardScheduler read from this at startup.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Verdichotomy/Game Config")]
	public class GameConfig : ScriptableObject
	{
		[field: SerializeField, Header("Stats"), Tooltip("All stats in the game, in the order they should appear in the UI.")]
		public StatDefinition[] Stats { get; 
#if !UNITY_EDITOR
			private set; 
#else
			set;
#endif
		} = Array.Empty<StatDefinition>();

		[field: SerializeField, Header("Flags"), Tooltip("The single FlagRegistry asset for this game.")]
		public Registry FlagRegistry { get;  
#if !UNITY_EDITOR
			private set; 
#else
			set;
#endif 
		}

		[field: SerializeField, Header("Card Pools"), Tooltip("All pools the scheduler will consider. Order doesn't affect selection.")]
		public CardPoolData[] Pools { get; 
#if !UNITY_EDITOR
			private set; 
#else
			set;
#endif 
		} = Array.Empty<CardPoolData>();

		[field: SerializeField, Header("Death & Restart"), Tooltip("How many seconds to show the death screen before restarting.")]
		public float deathScreenDuration { get; private set;  } = 3f;

		[field: SerializeField, Header("Debug"), Tooltip("If true, logs scheduler decisions to the console.")]
		public bool verboseSchedulerLogging { get; private set; }
	}
}