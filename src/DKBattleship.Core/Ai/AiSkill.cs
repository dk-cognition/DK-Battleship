namespace DKBattleship.Core.Ai;

/// <summary>
/// The dials that separate one golf personality's play from another. Every character supplies its
/// own <see cref="AiSkill"/>, which is what makes the roster span easy to brutal.
/// </summary>
/// <param name="UseDensityHunt">
/// Hunt by weighing every legal placement of the clubs still in the bag instead of swinging at a
/// random cell — the mark of a player who reads the whole course.
/// </param>
/// <param name="UseParity">Restrict random hunting to a checkerboard, so no club can be missed.</param>
/// <param name="MistakeChance">
/// Probability of abandoning the best available swing for a random one. Also drops focus on a
/// wounded club, which is exactly how weaker players lose holes they had won.
/// </param>
public readonly record struct AiSkill(bool UseDensityHunt, bool UseParity, double MistakeChance)
{
    /// <summary>~80% win rate against <see cref="ReferenceClubPlayer"/>.</summary>
    public static AiSkill Tour { get; } = new(UseDensityHunt: true, UseParity: true, MistakeChance: 0.00);

    /// <summary>~60% win rate against <see cref="ReferenceClubPlayer"/>.</summary>
    public static AiSkill Elite { get; } = new(UseDensityHunt: false, UseParity: true, MistakeChance: 0.25);

    /// <summary>~40% win rate against <see cref="ReferenceClubPlayer"/>.</summary>
    public static AiSkill Amateur { get; } = new(UseDensityHunt: false, UseParity: true, MistakeChance: 0.60);

    /// <summary>~20% win rate against <see cref="ReferenceClubPlayer"/>.</summary>
    public static AiSkill Casual { get; } = new(UseDensityHunt: false, UseParity: false, MistakeChance: 0.78);

    /// <summary>
    /// The yardstick the roster is calibrated against: an average human who sweeps at random, chases
    /// a wounded club when it spots one, and gets distracted a fair amount of the time. Character win
    /// rates in the roster are all measured against this model.
    /// </summary>
    public static AiSkill ReferenceClubPlayer { get; } =
        new(UseDensityHunt: false, UseParity: false, MistakeChance: 0.40);
}
