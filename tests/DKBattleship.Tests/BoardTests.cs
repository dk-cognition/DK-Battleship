using DKBattleship.Core;
using Xunit;

namespace DKBattleship.Tests;

public class BoardTests
{
    [Fact]
    public void NewBoard_IsEmpty()
    {
        var board = new Board();
        Assert.Equal(10, board.Rows);
        Assert.Equal(10, board.Cols);
        Assert.Empty(board.Ships);
        Assert.Equal(CellState.Empty, board.StateAt(0, 0));
        Assert.Equal(CellState.Empty, board.StateAt(9, 9));
    }

    [Fact]
    public void InBounds_RejectsCellsOutsideGrid()
    {
        var board = new Board(10, 10);
        Assert.True(board.InBounds(new Coordinate(0, 0)));
        Assert.True(board.InBounds(new Coordinate(9, 9)));
        Assert.False(board.InBounds(new Coordinate(10, 0)));
        Assert.False(board.InBounds(new Coordinate(0, 10)));
        Assert.False(board.InBounds(new Coordinate(-1, 5)));
        Assert.False(board.InBounds(new Coordinate(5, -1)));
    }

    [Fact]
    public void PlaceShip_SucceedsAtBoardEdge()
    {
        var board = new Board();
        var putter = new Ship("Putter", 2);

        Assert.True(board.PlaceShip(putter, new Coordinate(9, 8), Orientation.Horizontal));
        Assert.Equal(CellState.Ship, board.StateAt(9, 8));
        Assert.Equal(CellState.Ship, board.StateAt(9, 9));
    }

    [Fact]
    public void PlaceShip_RejectsHorizontalOverhang()
    {
        var board = new Board();
        var driver = new Ship("Driver", 5);

        Assert.False(board.CanPlace(driver, new Coordinate(0, 6), Orientation.Horizontal));
        Assert.False(board.PlaceShip(driver, new Coordinate(0, 6), Orientation.Horizontal));
        Assert.Empty(board.Ships);
        Assert.Equal(CellState.Empty, board.StateAt(0, 6));
    }

    [Fact]
    public void PlaceShip_RejectsVerticalOverhang()
    {
        var board = new Board();
        var wood = new Ship("Fairway Wood", 4);

        Assert.False(board.PlaceShip(wood, new Coordinate(7, 3), Orientation.Vertical));
        Assert.Empty(board.Ships);
    }

    [Fact]
    public void PlaceShip_RejectsOverlap()
    {
        var board = new Board();
        Assert.True(board.PlaceShip(new Ship("Driver", 5), new Coordinate(4, 0), Orientation.Horizontal));

        // Crosses the driver at (4,2).
        Assert.False(board.PlaceShip(new Ship("Hybrid", 3), new Coordinate(3, 2), Orientation.Vertical));
        Assert.Single(board.Ships);
    }

    [Fact]
    public void PlaceShip_RejectsPartialOverlapOnLastCell()
    {
        var board = new Board();
        Assert.True(board.PlaceShip(new Ship("Putter", 2), new Coordinate(0, 3), Orientation.Horizontal));
        Assert.False(board.PlaceShip(new Ship("Iron", 3), new Coordinate(0, 1), Orientation.Horizontal));
        Assert.Single(board.Ships);
    }

    [Fact]
    public void ReceiveShot_ReturnsMissOnEmptyCell()
    {
        var board = new Board();
        board.PlaceShip(new Ship("Putter", 2), new Coordinate(0, 0), Orientation.Horizontal);

        Assert.Equal(ShotResult.Miss, board.ReceiveShot(new Coordinate(5, 5)));
        Assert.Equal(CellState.Miss, board.StateAt(5, 5));
    }

    [Fact]
    public void ReceiveShot_ReturnsHitThenSunk()
    {
        var board = new Board();
        var putter = new Ship("Putter", 2);
        board.PlaceShip(putter, new Coordinate(2, 2), Orientation.Vertical);

        Assert.Equal(ShotResult.Hit, board.ReceiveShot(new Coordinate(2, 2)));
        Assert.False(putter.IsSunk);
        Assert.Equal(ShotResult.Sunk, board.ReceiveShot(new Coordinate(3, 2)));
        Assert.True(putter.IsSunk);
        Assert.Equal(CellState.Hit, board.StateAt(3, 2));
    }

    [Fact]
    public void ReceiveShot_ReturnsAlreadyShotForRepeats()
    {
        var board = new Board();
        var putter = new Ship("Putter", 2);
        board.PlaceShip(putter, new Coordinate(2, 2), Orientation.Vertical);

        Assert.Equal(ShotResult.Miss, board.ReceiveShot(new Coordinate(7, 7)));
        Assert.Equal(ShotResult.AlreadyShot, board.ReceiveShot(new Coordinate(7, 7)));

        Assert.Equal(ShotResult.Hit, board.ReceiveShot(new Coordinate(2, 2)));
        Assert.Equal(ShotResult.AlreadyShot, board.ReceiveShot(new Coordinate(2, 2)));
        Assert.False(putter.IsSunk);
    }

    [Fact]
    public void ReceiveShot_OutOfBoundsThrows()
    {
        var board = new Board();
        Assert.Throws<ArgumentOutOfRangeException>(() => board.ReceiveShot(new Coordinate(10, 10)));
    }

    [Fact]
    public void AllShipsSunk_FalseOnEmptyBoardAndUntilEveryShipIsSunk()
    {
        var board = new Board();
        Assert.False(board.AllShipsSunk);

        var putter = new Ship("Putter", 2);
        var iron = new Ship("Iron", 3);
        board.PlaceShip(putter, new Coordinate(0, 0), Orientation.Horizontal);
        board.PlaceShip(iron, new Coordinate(2, 0), Orientation.Horizontal);

        board.ReceiveShot(new Coordinate(0, 0));
        board.ReceiveShot(new Coordinate(0, 1));
        Assert.True(putter.IsSunk);
        Assert.False(board.AllShipsSunk);

        board.ReceiveShot(new Coordinate(2, 0));
        board.ReceiveShot(new Coordinate(2, 1));
        Assert.Equal(ShotResult.Sunk, board.ReceiveShot(new Coordinate(2, 2)));
        Assert.True(board.AllShipsSunk);
    }

    [Fact]
    public void Board_SupportsNonDefaultDimensions()
    {
        var board = new Board(6, 8);
        Assert.True(board.InBounds(new Coordinate(5, 7)));
        Assert.False(board.InBounds(new Coordinate(6, 7)));
        Assert.False(board.CanPlace(new Ship("Driver", 5), new Coordinate(2, 5), Orientation.Horizontal));
        Assert.True(board.CanPlace(new Ship("Driver", 5), new Coordinate(1, 1), Orientation.Vertical));
    }
}
