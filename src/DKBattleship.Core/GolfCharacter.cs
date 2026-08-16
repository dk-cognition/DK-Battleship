namespace DKBattleship.Core;

using DKBattleship.Core.Ai;

/// <summary>
/// A golf personality the player can tee off against. Characters are the extension point for
/// future opponents: each one supplies its own <see cref="IAiPlayer"/> strategy and flavour text.
/// </summary>
public class GolfCharacter
{
    public GolfCharacter(
        string name,
        string title,
        string description,
        string avatar,
        Func<Random, IAiPlayer> strategyFactory,
        IReadOnlyList<string>? taunts = null)
    {
        Name = name;
        Title = title;
        Description = description;
        Avatar = avatar;
        StrategyFactory = strategyFactory;
        Taunts = taunts ?? Array.Empty<string>();
    }

    public string Name { get; }

    public string Title { get; }

    public string Description { get; }

    /// <summary>Short emoji/text avatar so the UI needs no image assets.</summary>
    public string Avatar { get; }

    public Func<Random, IAiPlayer> StrategyFactory { get; }

    public IReadOnlyList<string> Taunts { get; }

    public IAiPlayer CreateStrategy(Random random) => StrategyFactory(random);
}

/// <summary>Seed roster; more characters (with distinct strategies) can be added here later.</summary>
public static class GolfCharacters
{
    public static GolfCharacter ThePro { get; } = new(
        name: "The Pro",
        title: "Tour Champion",
        description: "Reads the course like a green book. Sweeps efficiently and never wastes a swing.",
        avatar: "\U0001F3CC\uFE0F",
        strategyFactory: random => new HuntTargetAi("The Pro", random, useParity: true),
        taunts: new[] { "That's how you strike a ball.", "Textbook. Next hole." });

    public static GolfCharacter TheWeekendHacker { get; } = new(
        name: "The Weekend Hacker",
        title: "Saturday Regular",
        description: "Big swings, questionable course management. Sprays it around before finding the target.",
        avatar: "\U0001F3CC",
        strategyFactory: random => new HuntTargetAi("The Weekend Hacker", random, useParity: false),
        taunts: new[] { "Fore! ...sorry about that.", "Lucky bounce, I'll take it." });

    public static IReadOnlyList<GolfCharacter> All { get; } = new[] { ThePro, TheWeekendHacker };
}
