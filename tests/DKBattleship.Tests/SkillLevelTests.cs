namespace DKBattleship.Tests;

using DKBattleship.Core;
using DKBattleship.Core.Ai;
using Xunit;

/// <summary>
/// Locks in the roster's difficulty ladder. Win rates are measured by simulating full matches
/// against <see cref="MatchSimulator.ReferenceSkill"/>, so a tweak to any skill dial that pushes a
/// character off its advertised difficulty fails the build.
/// </summary>
public class SkillLevelTests
{
    private const int Matches = 500;
    private const double Tolerance = 0.07;

    /// <summary>
    /// Density hunting is only worth its cost if it clears a course quickly; a bookkeeping slip (e.g.
    /// counting placements across cells belonging to a club already in the hole) shows up here as extra
    /// swings long before it moves the win rate.
    /// </summary>
    [Fact]
    public void Tiger_ClearsACourseInFarFewerSwingsThanKyle()
    {
        var tiger = AverageSwingsToClear(GolfCharacters.TigerWoods, matches: 100, seed: 31);
        var kyle = AverageSwingsToClear(GolfCharacters.KyleStalder, matches: 100, seed: 31);

        Assert.InRange(tiger, 40, 52);
        Assert.True(kyle > tiger + 20, $"Kyle averaged {kyle:F1} swings vs Tiger's {tiger:F1}");
    }

    private static double AverageSwingsToClear(GolfCharacter character, int matches, int seed)
    {
        var random = new Random(seed);
        var total = 0;

        for (var i = 0; i < matches; i++)
        {
            var board = new Board();
            ShipPlacer.PlaceRandomly(board, Ship.CreateGolfBag(), random);
            var ai = character.CreateStrategy(random);
            var view = new BoardView(board);

            while (!board.AllShipsSunk)
            {
                MatchSimulator.Fire(ai, board, ai.NextShot(view));
                total++;
            }
        }

        return (double)total / matches;
    }

    [Theory]
    [InlineData("Tiger Woods", 0.80)]
    [InlineData("Jordan Spieth", 0.60)]
    [InlineData("Jackson Koivun", 0.40)]
    [InlineData("Kyle Stalder", 0.20)]
    public void Character_WinsRoughlyItsAdvertisedShareOfMatches(string name, double expected)
    {
        var character = GolfCharacters.All.Single(c => c.Name == name);
        Assert.Equal(expected, character.ExpectedWinRate, precision: 2);

        var measured = MatchSimulator.WinRate(character, Matches, seed: 20260816);

        Assert.InRange(measured, expected - Tolerance, expected + Tolerance);
    }

    [Fact]
    public void Roster_IsOrderedHardestToEasiest()
    {
        var rates = GolfCharacters.All.Select(c => c.ExpectedWinRate).ToList();

        Assert.Equal(rates.OrderByDescending(r => r), rates);
        Assert.Equal("Tiger Woods", GolfCharacters.All[0].Name);
        Assert.Equal("The GOAT", GolfCharacters.TigerWoods.Title);
        Assert.Equal("Kyle Stalder", GolfCharacters.All[^1].Name);
    }

    [Fact]
    public void HarderCharacters_BeatEasierOnesHeadToHead()
    {
        var random = new Random(7);
        var tigerWins = 0;

        for (var i = 0; i < 60; i++)
        {
            // Kyle swings first, i.e. Tiger concedes the tee shot and still comes out ahead.
            if (MatchSimulator.PlayMatch(
                    GolfCharacters.TigerWoods.CreateStrategy(random),
                    GolfCharacters.KyleStalder.CreateStrategy(random),
                    random))
            {
                tigerWins++;
            }
        }

        Assert.True(tigerWins > 45, $"Tiger only won {tigerWins}/60 against Kyle");
    }
}
