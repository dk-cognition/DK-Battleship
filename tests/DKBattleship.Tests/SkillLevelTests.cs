namespace DKBattleship.Tests;

using DKBattleship.Core;
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
