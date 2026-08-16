namespace DKBattleship.Tests;

using DKBattleship.Core;
using DKBattleship.Core.Ai;

/// <summary>
/// Plays complete AI-vs-AI matches so character skill levels can be measured instead of guessed.
/// The human seat is filled by <see cref="ReferenceSkill"/>: an average club player who sweeps at
/// random, chases wounded clubs, and loses focus a fair amount of the time. Character win rates are
/// expressed against that model.
/// </summary>
public static class MatchSimulator
{
    /// <summary>The stand-in for an average human opponent.</summary>
    public static AiSkill ReferenceSkill => AiSkill.ReferenceClubPlayer;

    /// <summary>Fraction of matches the character's strategy wins against the reference player.</summary>
    public static double WinRate(GolfCharacter character, int matches, int seed)
    {
        var random = new Random(seed);
        var wins = 0;

        for (var i = 0; i < matches; i++)
        {
            if (PlayMatch(character.CreateStrategy(random), NewReferencePlayer(random), random))
            {
                wins++;
            }
        }

        return (double)wins / matches;
    }

    public static IAiPlayer NewReferencePlayer(Random random) =>
        new HuntTargetAi("Reference Club Player", random, ReferenceSkill);

    /// <summary>
    /// Runs one match. The reference player swings first, mirroring the web UI where the human
    /// opens the round. Returns true when <paramref name="character"/> wins.
    /// </summary>
    public static bool PlayMatch(IAiPlayer character, IAiPlayer reference, Random random)
    {
        var characterBoard = new Board();
        var referenceBoard = new Board();
        ShipPlacer.PlaceRandomly(characterBoard, Ship.CreateGolfBag(), random);
        ShipPlacer.PlaceRandomly(referenceBoard, Ship.CreateGolfBag(), random);

        var characterView = new BoardView(referenceBoard);
        var referenceView = new BoardView(characterBoard);

        while (true)
        {
            if (Swing(reference, referenceView, characterBoard))
            {
                return false;
            }

            if (Swing(character, characterView, referenceBoard))
            {
                return true;
            }
        }
    }

    /// <summary>Takes one swing; returns true when it finished the match.</summary>
    private static bool Swing(IAiPlayer shooter, BoardView view, Board target)
    {
        Fire(shooter, target, shooter.NextShot(view));
        return target.AllShipsSunk;
    }

    /// <summary>
    /// Fires at <paramref name="board"/> and feeds the outcome back, including the size of any club
    /// that went in the hole — the same call an opponent makes out loud.
    /// </summary>
    public static ShotResult Fire(IAiPlayer shooter, Board board, Coordinate shot)
    {
        var club = board.ShipAt(shot);
        var result = board.ReceiveShot(shot);
        shooter.RecordResult(shot, result, result == ShotResult.Sunk ? club!.Size : 0);
        return result;
    }
}
