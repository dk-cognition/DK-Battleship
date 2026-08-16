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
        double expectedWinRate,
        IReadOnlyList<string>? taunts = null)
    {
        Name = name;
        Title = title;
        Description = description;
        Avatar = avatar;
        StrategyFactory = strategyFactory;
        ExpectedWinRate = expectedWinRate;
        Taunts = taunts ?? Array.Empty<string>();
    }

    public string Name { get; }

    public string Title { get; }

    public string Description { get; }

    /// <summary>Short emoji/text avatar so the UI needs no image assets.</summary>
    public string Avatar { get; }

    public Func<Random, IAiPlayer> StrategyFactory { get; }

    /// <summary>
    /// Share of matches this character is tuned to win against the reference club player
    /// (see <see cref="AiSkill"/> and the win-rate tests). Drives the roster's difficulty copy.
    /// </summary>
    public double ExpectedWinRate { get; }

    public IReadOnlyList<string> Taunts { get; }

    public IAiPlayer CreateStrategy(Random random) => StrategyFactory(random);
}

/// <summary>
/// The roster, hardest first. Each entry pairs flavour text with the <see cref="AiSkill"/> dials
/// that produce its target win rate; new characters just add another entry.
/// </summary>
public static class GolfCharacters
{
    public static GolfCharacter TigerWoods { get; } = new(
        name: "Tiger Woods",
        title: "The GOAT",
        description: "The GOAT. Reads every yard of the course, never loses focus, and closes out holes you thought were safe.",
        avatar: "\U0001F405",
        strategyFactory: random => new HuntTargetAi("Tiger Woods", random, AiSkill.Tour),
        expectedWinRate: 0.80,
        taunts: new[] { "That's how you strike a ball.", "Textbook. Next hole." });

    public static GolfCharacter JordanSpieth { get; } = new(
        name: "Jordan Spieth",
        title: "Major Winner",
        description: "Streaky brilliance. Picks the course apart methodically, with the odd wild swing thrown in.",
        avatar: "\U0001F3CC\uFE0F",
        strategyFactory: random => new HuntTargetAi("Jordan Spieth", random, AiSkill.Elite),
        expectedWinRate: 0.60,
        taunts: new[] { "Go, go, go!", "Knew it was in from the moment I hit it." });

    public static GolfCharacter JacksonKoivun { get; } = new(
        name: "Jackson Koivun",
        title: "Amateur Standout",
        description: "College phenom with a huge ceiling. Sweeps the course well but still loses the plot on a few holes.",
        avatar: "\U0001F393",
        strategyFactory: random => new HuntTargetAi("Jackson Koivun", random, AiSkill.Amateur),
        expectedWinRate: 0.40,
        taunts: new[] { "Getting closer every round.", "That one felt good." });

    public static GolfCharacter KyleStalder { get; } = new(
        name: "Kyle Stalder",
        title: "Weekend Regular",
        description: "Out here for the beer cart and the sunshine. Still working on fixing that slice.",
        avatar: "\U0001F3CC",
        strategyFactory: random => new HuntTargetAi("Kyle Stalder", random, AiSkill.Casual),
        expectedWinRate: 0.20,
        taunts: new[] { "Fore! ...sorry about that.", "Lucky bounce, I'll take it." });

    public static IReadOnlyList<GolfCharacter> All { get; } =
        new[] { TigerWoods, JordanSpieth, JacksonKoivun, KyleStalder };
}
