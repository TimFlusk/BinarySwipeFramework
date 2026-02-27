using System;
using ReignsFramework.Runtime;
using UnityEngine;
namespace VerdichotomyFramework.Cards.Condition
{
	/// <summary>
	/// All child conditions must be true.
	/// </summary>
	[Serializable]
	public class AndCondition : CardCondition
	{
		[SerializeReference]
		public CardCondition[] conditions = Array.Empty<CardCondition>();

		public override bool Evaluate(GameStateManager state)
		{
			foreach (var c in conditions)
			{
				if (c != null && !c.Evaluate(state)) return ApplyInvert(false);
			}
			return ApplyInvert(true);
		}
	}
}