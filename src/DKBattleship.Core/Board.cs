namespace DKBattleship.Core;

/// <summary>A course: grid of cells holding club placements and the swings taken at them.</summary>
public class Board
{
    public const int DefaultSize = 10;

    private readonly CellState[,] _cells;
    private readonly List<Ship> _ships = new();

    public Board(int rows = DefaultSize, int cols = DefaultSize)
    {
        if (rows <= 0 || cols <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "Board dimensions must be positive.");
        }

        Rows = rows;
        Cols = cols;
        _cells = new CellState[rows, cols];
    }

    public int Rows { get; }

    public int Cols { get; }

    public IReadOnlyList<Ship> Ships => _ships;

    public bool AllShipsSunk => _ships.Count > 0 && _ships.All(s => s.IsSunk);

    public bool InBounds(Coordinate c) => c.Row >= 0 && c.Row < Rows && c.Col >= 0 && c.Col < Cols;

    public CellState this[Coordinate c] =>
        InBounds(c) ? _cells[c.Row, c.Col] : throw new ArgumentOutOfRangeException(nameof(c), $"{c} is off the course.");

    public CellState StateAt(int row, int col) => this[new Coordinate(row, col)];

    /// <summary>Cells a ship of <paramref name="size"/> would occupy starting at <paramref name="start"/>.</summary>
    public static IEnumerable<Coordinate> Span(Coordinate start, int size, Orientation orientation)
    {
        for (var i = 0; i < size; i++)
        {
            yield return orientation == Orientation.Horizontal
                ? start.Offset(0, i)
                : start.Offset(i, 0);
        }
    }

    public bool CanPlace(Coordinate start, int size, Orientation orientation)
    {
        if (size <= 0)
        {
            return false;
        }

        foreach (var c in Span(start, size, orientation))
        {
            if (!InBounds(c) || _cells[c.Row, c.Col] != CellState.Empty)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// A ship instance belongs to exactly one board position: an already placed club cannot be
    /// re-placed, which would otherwise leave its previous cells occupied forever.
    /// </summary>
    public bool CanPlace(Ship ship, Coordinate start, Orientation orientation) =>
        !ship.IsPlaced && !_ships.Contains(ship) && CanPlace(start, ship.Size, orientation);

    /// <summary>Places a ship, returning false when the placement is invalid (out of bounds or overlapping).</summary>
    public bool PlaceShip(Ship ship, Coordinate start, Orientation orientation)
    {
        if (!CanPlace(ship, start, orientation))
        {
            return false;
        }

        var span = Span(start, ship.Size, orientation).ToList();
        ship.Place(span);
        foreach (var c in span)
        {
            _cells[c.Row, c.Col] = CellState.Ship;
        }

        _ships.Add(ship);
        return true;
    }

    public ShotResult ReceiveShot(Coordinate c)
    {
        if (!InBounds(c))
        {
            throw new ArgumentOutOfRangeException(nameof(c), $"{c} is off the course.");
        }

        var state = _cells[c.Row, c.Col];
        if (state is CellState.Hit or CellState.Miss)
        {
            return ShotResult.AlreadyShot;
        }

        if (state == CellState.Empty)
        {
            _cells[c.Row, c.Col] = CellState.Miss;
            return ShotResult.Miss;
        }

        _cells[c.Row, c.Col] = CellState.Hit;
        var ship = _ships.First(s => s.Occupies(c));
        ship.RegisterHit(c);
        return ship.IsSunk ? ShotResult.Sunk : ShotResult.Hit;
    }

    /// <summary>The ship occupying a cell, if any. Useful for UI copy ("Putter is in the hole!").</summary>
    public Ship? ShipAt(Coordinate c) => _ships.FirstOrDefault(s => s.Occupies(c));
}
