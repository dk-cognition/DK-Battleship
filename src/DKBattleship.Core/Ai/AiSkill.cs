namespace DKBattleship.Core.Ai;

/// <summary>
/// The dials that separate one golf personality's play from another. Every character supplies its
/// own <see cref="AiSkill"/>, which is what makes the roster span easy to brutal.
/// </summary>
/// <param name="UseDensityHunt">
/// Hunt by weighing every legal placement of the clubs still in the bag instead of swinging at a
/// random cell — the mark of a player who reads the whole course.
/// </param>
/// <param name="UseParity">
/// Restrict random hunting to a lattice spaced by the shortest club still afloat, so no club can be
/// missed while covering the course in fewer swings.
/// </param>
/// <param name="MistakeChance">
/// Per-swing probability of a lapse in concentration: the swing goes to a random untried cell,
/// ignoring both the target queue (walking away from a wounded club) and the hunting logic above.
/// This is what separates the roster more than anything else.
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
