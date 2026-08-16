using DKBattleship.Core;
using DKBattleship.Core.Ai;
using Xunit;

namespace DKBattleship.Tests;

/// <summary>Regression coverage for the defects recorded in DEBUGGING.md.</summary>
public class RegressionTests
{
    [Fact]
    public void PlacingTheSameShipInstanceTwice_IsRejected()
    {
        var board = new Board();
        var ship = new Ship("Putter", 2);
        Assert.True(board.PlaceShip(ship, new Coordinate(0, 0), Orientation.Horizontal));
        var second = board.PlaceShip(ship, new Coordinate(5, 5), Orientation.Horizontal);
        Assert.False(second);
        Assert.Single(board.Ships);
        Assert.Equal(CellState.Empty, board.StateAt(5, 5));
    }

    [Fact]
    public void RandomPlacement_CompletesABagThatWasPartiallyPlacedByHand()
    {
        var game = new Game(random: new Random(8));
        Assert.True(game.PlacePlayerShip(new Coordinate(0, 0), Orientation.Horizontal));
        game.RandomizePlayerShips();
        Assert.Equal(5, game.PlayerBoard.Ships.Count);
        Assert.Equal(GameStatus.PlayerTurn, game.Status);
    }

    [Fact]
    public void SinkingAShip_KeepsWorkingAHitOnANeighbouringShip()
    {
        var board = new Board();
        var putter = new Ship("Putter", 2);
        var iron = new Ship("Iron", 3);
        Assert.True(board.PlaceShip(putter, new Coordinate(4, 4), Orientation.Horizontal));
        Assert.True(board.PlaceShip(iron, new Coordinate(5, 4), Orientation.Horizontal));
        var ai = new HuntTargetAi(random: new Random(1));

        ai.RecordResult(new Coordinate(4, 4), board.ReceiveShot(new Coordinate(4, 4))); // Hit putter
        ai.RecordResult(new Coordinate(5, 4), board.ReceiveShot(new Coordinate(5, 4))); // Hit iron below
        ai.RecordResult(new Coordinate(4, 5), board.ReceiveShot(new Coordinate(4, 5))); // Sinks putter

        // The iron is still wounded at (5,4); a smart AI should keep working it.
        Assert.Equal(AiMode.Target, ai.Mode);
    }

    [Fact]
    public void SinkingAClubInLineWithAnother_LeavesTheNeighboursHitsOpen()
    {
        var board = new Board();
        var bag = Ship.CreateGolfBag();
        var putter = bag.Single(s => s.Name == "Putter");
        var hybrid = bag.Single(s => s.Name == "Hybrid");
        Assert.True(board.PlaceShip(putter, new Coordinate(0, 0), Orientation.Horizontal)); // A1-B1
        Assert.True(board.PlaceShip(hybrid, new Coordinate(0, 2), Orientation.Horizontal)); // C1-E1
        var ai = new HuntTargetAi("Probe", new Random(1), AiSkill.Tour);

        foreach (var col in new[] { 2, 3, 0, 1 })
        {
            MatchSimulator.Fire(ai, board, new Coordinate(0, col));
        }

        Assert.True(putter.IsSunk);
        Assert.False(hybrid.IsSunk);

        // The hits at C1/D1 belong to the hybrid, so the AI must keep working that lead.
        Assert.Equal(AiMode.Target, ai.Mode);
        Assert.Contains(new Coordinate(0, 4), ai.TargetQueue);
    }

    [Fact]
    public void NextShot_NeverRepeatsWhenResultsAreNotRecordedYet()
    {
        var board = new Board();
        board.PlaceShip(new Ship("Putter", 2), new Coordinate(0, 0), Orientation.Horizontal);
        var ai = new HuntTargetAi(random: new Random(6));
        var view = new BoardView(board);

        var issued = new HashSet<Coordinate>();
        for (var i = 0; i < 30; i++)
        {
            var shot = ai.NextShot(view);
            Assert.True(issued.Add(shot), $"AI offered {shot} twice before any result was recorded");
        }
    }

    [Fact]
    public void BoardTooSmallForTheBag_FailsLoudly()
    {
        Assert.Throws<InvalidOperationException>(() => new Game(random: new Random(1), rows: 4, cols: 4));
    }

    [Fact]
    public void AiFire_AfterGameOver_Throws()
    {
        var game = new Game(random: new Random(3));
        game.RandomizePlayerShips();
        foreach (var target in game.AiBoard.Ships.SelectMany(s => s.Coordinates).Distinct().ToList())
        {
            if (game.IsOver)
            {
                break;
            }

            game.PlayerFire(target);
            if (game.Status == GameStatus.AiTurn)
            {
                game.AiFire();
            }
        }

        Assert.Equal(GameStatus.PlayerWon, game.Status);
        Assert.Throws<InvalidOperationException>(() => game.AiFire());
    }
}
