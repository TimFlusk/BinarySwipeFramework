namespace VerdichotomyFramework.Cards.Data
{
	/// <summary>
	/// Definition for detailing how frequently a card will appear in a cycle
	/// </summary>
	public enum Recurrence
	{
		/// <summary>Shown once, then removed from the pool forever (this run).</summary>
		OneShot,
		/// <summary>Can appear any number of times, subject to cooldown.</summary>
		Repeatable,
		/// <summary>Cycles through variants sequentially (see CardData.variants).</summary>
		Cycling
	}
}