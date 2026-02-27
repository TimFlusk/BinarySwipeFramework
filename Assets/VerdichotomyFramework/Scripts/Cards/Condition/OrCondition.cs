using System;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Condition
{
	/// <summary>
	/// At least one child condition must be true.
	/// </summary>
	[Serializable]
	public class OrCondition : CardCondition
	{
		[SerializeReference]
		public CardCondition[] conditions = Array.Empty<CardCondition>();

		public override bool Evaluate(GameStateManager state)
		{
			foreach (var c in conditions)
			{
				if (c != null && c.Evaluate(state)) return ApplyInvert(true);
			}
			return ApplyInvert(false);
		}
	}
}