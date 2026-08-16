using DKBattleship.Core;
using DKBattleship.Core.Ai;
using Xunit;

namespace DKBattleship.Tests;

public class AiTests
{
    private static Board BoardWithSingleShip(Coordinate start, int size, Orientation orientation, out Ship ship)
    {
        var board = new Board();
        ship = new Ship("Iron", size);
        Assert.True(board.PlaceShip(ship, start, orientation));
        return board;
    }

    [Fact]
    public void NeverRepeatsAShot_OverAFullBoard()
    {
        var board = new Board();
        board.PlaceShip(new Ship("Driver", 5), new Coordinate(0, 0), Orientation.Horizontal);
        var ai = new HuntTargetAi(random: new Random(7));
        var view = new BoardView(board);
        var seen = new HashSet<Coordinate>();

        for (var i = 0; i < board.Rows * board.Cols; i++)
        {
            var shot = ai.NextShot(view);
            Assert.True(board.InBounds(shot), $"shot {shot} was off the course");
            Assert.True(seen.Add(shot), $"shot {shot} was repeated");
            var result = board.ReceiveShot(shot);
            Assert.NotEqual(ShotResult.AlreadyShot, result);
            ai.RecordResult(shot, result);
        }

        Assert.Equal(100, seen.Count);
        Assert.Throws<InvalidOperationException>(() => ai.NextShot(view));
    }

    [Fact]
    public void SwitchesToTargetModeAfterHit_AndQueuesOnlyAdjacentCells()
    {
        var board = BoardWithSingleShip(new Coordinate(4, 4), 3, Orientation.Horizontal, out _);
        var ai = new HuntTargetAi(random: new Random(1));
        Assert.Equal(AiMode.Hunt, ai.Mode);

        var hit = new Coordinate(4, 4);
        ai.RecordResult(hit, board.ReceiveShot(hit));

        Assert.Equal(AiMode.Target, ai.Mode);
        Assert.All(ai.TargetQueue, c => Assert.Contains(c, hit.Neighbours().ToList()));

        var next = ai.NextShot(new BoardView(board));
        Assert.Contains(next, hit.Neighbours().ToList());
    }

    [Fact]
    public void TargetMode_NeverReturnsOffBoardCellsForCornerHits()
    {
        var board = BoardWithSingleShip(new Coordinate(0, 0), 3, Orientation.Horizontal, out _);
        var ai = new HuntTargetAi(random: new Random(3));

        var hit = new Coordinate(0, 0);
        ai.RecordResult(hit, board.ReceiveShot(hit));

        for (var i = 0; i < 4 && ai.Mode == AiMode.Target; i++)
        {
            var shot = ai.NextShot(new BoardView(board));
            Assert.True(board.InBounds(shot), $"{shot} is off the course");
            ai.RecordResult(shot, board.ReceiveShot(shot));
        }
    }

    [Fact]
    public void ReturnsToHuntModeAfterSinking()
    {
        var board = BoardWithSingleShip(new Coordinate(5, 5), 2, Orientation.Horizontal, out _);
        var ai = new HuntTargetAi(random: new Random(11));

        ai.RecordResult(new Coordinate(5, 5), board.ReceiveShot(new Coordinate(5, 5)));
        Assert.Equal(AiMode.Target, ai.Mode);

        ai.RecordResult(new Coordinate(5, 6), board.ReceiveShot(new Coordinate(5, 6)));
        Assert.Equal(AiMode.Hunt, ai.Mode);
        Assert.Empty(ai.TargetQueue);
    }

    [Fact]
    public void TargetMode_SkipsCellsAlreadyShot()
    {
        var board = BoardWithSingleShip(new Coordinate(4, 4), 3, Orientation.Horizontal, out _);
        var ai = new HuntTargetAi(random: new Random(5));

        // Player-independent: the AI itself already tried the cell above the upcoming hit.
        var above = new Coordinate(3, 4);
        ai.RecordResult(above, board.ReceiveShot(above));

        var hit = new Coordinate(4, 4);
        ai.RecordResult(hit, board.ReceiveShot(hit));

        Assert.DoesNotContain(above, ai.TargetQueue);
        var next = ai.NextShot(new BoardView(board));
        Assert.NotEqual(above, next);
    }

    [Fact]
    public void ParityHunting_OnlyPicksCheckerboardCellsWhileAvailable()
    {
        var board = new Board();
        board.PlaceShip(new Ship("Putter", 2), new Coordinate(0, 0), Orientation.Horizontal);
        var ai = new HuntTargetAi(random: new Random(13), useParity: true);
        var view = new BoardView(board);

        for (var i = 0; i < 20; i++)
        {
            var shot = ai.NextShot(view);
            if (ai.Mode == AiMode.Hunt)
            {
                Assert.True((shot.Row + shot.Col) % 2 == 0, $"{shot} is off the parity grid");
            }

            ai.RecordResult(shot, board.ReceiveShot(shot));
        }
    }

    [Fact]
    public void Reset_ClearsHistorySoTheInstanceCanReplay()
    {
        var board = new Board();
        board.PlaceShip(new Ship("Putter", 2), new Coordinate(0, 0), Orientation.Horizontal);
        var ai = new HuntTargetAi(random: new Random(2));

        var shot = ai.NextShot(new BoardView(board));
        ai.RecordResult(shot, ShotResult.Hit);
        Assert.NotEmpty(ai.ShotsTaken);

        ai.Reset();
        Assert.Empty(ai.ShotsTaken);
        Assert.Equal(AiMode.Hunt, ai.Mode);
    }

    [Fact]
    public void BoardView_DoesNotRevealUnhitShips()
    {
        var board = BoardWithSingleShip(new Coordinate(2, 2), 3, Orientation.Vertical, out _);
        var view = new BoardView(board);

        Assert.False(view.WasShot(new Coordinate(2, 2)));
        Assert.Equal(100, view.UntriedCells().Count());
        board.ReceiveShot(new Coordinate(2, 2));
        Assert.True(view.WasShot(new Coordinate(2, 2)));
        Assert.Equal(99, view.UntriedCells().Count());
    }

    [Fact]
    public void FullGame_AiSinksEveryShipWithoutRepeating()
    {
        var random = new Random(42);
        var board = new Board();
        ShipPlacer.PlaceRandomly(board, Ship.CreateGolfBag(), random);
        var ai = new HuntTargetAi(random: random);
        var view = new BoardView(board);
        var swings = 0;

        while (!board.AllShipsSunk)
        {
            var shot = ai.NextShot(view);
            var result = board.ReceiveShot(shot);
            Assert.NotEqual(ShotResult.AlreadyShot, result);
            ai.RecordResult(shot, result);
            swings++;
            Assert.True(swings <= 100, "AI should finish within one pass of the course");
        }

        Assert.True(board.AllShipsSunk);
    }
}
