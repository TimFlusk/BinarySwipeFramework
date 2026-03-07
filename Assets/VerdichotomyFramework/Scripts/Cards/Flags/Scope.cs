namespace VerdichotomyFramework.Cards.Flags
{
	/// <summary>
	/// Defines whether this is tracked per session or the duration of the player's session
	/// </summary>
	public enum Scope
	{
		/// <summary>Resets when the player dies / starts a new run.</summary>
		Run,
		/// <summary>Persists across runs for the campaign meta-story.</summary>
		Campaign
	}
}