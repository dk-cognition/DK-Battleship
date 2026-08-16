using DKBattleship.Core;
using Xunit;

namespace DKBattleship.Tests;

public class ShipTests
{
    [Fact]
    public void GolfBag_MatchesStandardBattleshipSizes()
    {
        var bag = Ship.CreateGolfBag();
        Assert.Equal(new[] { 5, 4, 3, 3, 2 }, bag.Select(s => s.Size));
        Assert.Equal(
            new[] { "Driver", "Fairway Wood", "Hybrid", "Iron", "Putter" },
            bag.Select(s => s.Name));
        Assert.All(bag, s => Assert.False(s.IsPlaced));
    }

    [Fact]
    public void CreateGolfBag_ReturnsFreshInstancesEachCall()
    {
        var first = Ship.CreateGolfBag();
        var second = Ship.CreateGolfBag();
        first[0].Place(Board.Span(new Coordinate(0, 0), 5, Orientation.Horizontal));

        Assert.True(first[0].IsPlaced);
        Assert.False(second[0].IsPlaced);
    }

    [Fact]
    public void UnplacedShip_IsNotSunk()
    {
        var ship = new Ship("Putter", 2);
        Assert.False(ship.IsSunk);
    }

    [Fact]
    public void RegisterHit_IgnoresCellsOutsideShipAndDuplicates()
    {
        var ship = new Ship("Putter", 2);
        ship.Place(Board.Span(new Coordinate(1, 1), 2, Orientation.Horizontal));

        Assert.False(ship.RegisterHit(new Coordinate(5, 5)));
        Assert.True(ship.RegisterHit(new Coordinate(1, 1)));
        Assert.False(ship.RegisterHit(new Coordinate(1, 1)));
        Assert.False(ship.IsSunk);
        Assert.True(ship.RegisterHit(new Coordinate(1, 2)));
        Assert.True(ship.IsSunk);
    }

    [Fact]
    public void Place_RejectsWrongNumberOfCells()
    {
        var ship = new Ship("Iron", 3);
        Assert.Throws<ArgumentException>(() => ship.Place(Board.Span(new Coordinate(0, 0), 2, Orientation.Horizontal)));
    }

    [Fact]
    public void Coordinate_UsesScorecardLabels()
    {
        Assert.Equal("A1", new Coordinate(0, 0).ToString());
        Assert.Equal("J10", new Coordinate(9, 9).ToString());
    }
}
