using System;
using UnityEngine;
namespace VerdichotomyFramework
{
    /// <summary>
    ///     Represents a character who presents cards to the player.
    ///     Artists attach art assets here; designers reference this from CardData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Reigns/Character")]
	public class CharacterData : ScriptableObject
	{
		[Header("Identity")]
		public string characterName;

		[TextArea(1, 3), Tooltip("Optional flavour bio shown in a character log / codex.")]
		public string biography;

		[Header("Art"), Tooltip("Main portrait sprite shown on the card.")]
		public Sprite portrait;

		[Tooltip("Optional animated portrait (overrides portrait if set).")]
		public RuntimeAnimatorController animatorController;

		[Tooltip("Background or scene sprite shown behind the character.")]
		public Sprite backgroundSprite;

		[Header("Audio"), Tooltip("Ambient audio loop played while this character's card is shown.")]
		public AudioClip ambientClip;

		[Tooltip("Clips played at random when the card is dealt.")]
		public AudioClip[] dealSounds = Array.Empty<AudioClip>();

		[Header("Layout"), Tooltip("Horizontal offset of the portrait within the card frame (for framing art).")]
		public Vector2 portraitOffset = Vector2.zero;

		[Tooltip("Scale multiplier for the portrait.")]
		public float portraitScale = 1f;
	}
}