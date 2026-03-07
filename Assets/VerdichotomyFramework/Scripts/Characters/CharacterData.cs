using System;
using UnityEngine;
namespace VerdichotomyFramework.Characters
{
    /// <summary>
    ///     Represents a character who presents cards to the player.
    ///     Artists attach art assets here; designers reference this from CardData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Reigns/Character")]
	public class CharacterData : ScriptableObject
	{
		[field: SerializeField, Header("Identity")]
		public string characterName { get; private set; }

		[field: SerializeField, TextArea(1, 3), Tooltip("Optional flavour bio shown in a character log / codex.")]
		public string biography { get; private set; }

		[field: SerializeField, Header("Art"), Tooltip("Main portrait sprite shown on the card.")]
		public Sprite portrait { get; private set; }

		[field: SerializeField, Tooltip("Optional animated portrait (overrides portrait if set).")]
		public RuntimeAnimatorController animatorController { get; private set; }

		[field: SerializeField, Tooltip("Background or scene sprite shown behind the character.")]
		public Sprite backgroundSprite { get; private set; }

		[field: SerializeField, Header("Audio"), Tooltip("Ambient audio loop played while this character's card is shown.")]
		public AudioClip ambientClip { get; private set; }

		[field: SerializeField, Tooltip("Clips played at random when the card is dealt.")]
		public AudioClip[] dealSounds  { get; private set; } = Array.Empty<AudioClip>();

		[field: SerializeField, Header("Layout"), Tooltip("Horizontal offset of the portrait within the card frame (for framing art).")]
		public Vector2 portraitOffset  { get; private set; } = Vector2.zero;

		[field: SerializeField, Tooltip("Scale multiplier for the portrait.")]
		public float portraitScale  { get; private set; } = 1f;
	}
}