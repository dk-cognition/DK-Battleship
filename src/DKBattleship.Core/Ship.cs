namespace DKBattleship.Core;

/// <summary>A golf club in the bag: occupies <see cref="Size"/> contiguous cells.</summary>
public class Ship
{
    private readonly HashSet<Coordinate> _hits = new();

    public Ship(string name, int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "A club must cover at least one cell.");
        }

        Name = name;
        Size = size;
    }

    public string Name { get; }

    public int Size { get; }

    /// <summary>Cells the ship occupies once placed; empty while it is still in the bag.</summary>
    public IReadOnlyCollection<Coordinate> Coordinates => _coordinates;

    private readonly List<Coordinate> _coordinates = new();

    public IReadOnlyCollection<Coordinate> Hits => _hits;

    public bool IsPlaced => _coordinates.Count == Size;

    public bool IsSunk => IsPlaced && _hits.Count == Size;

    public void Place(IEnumerable<Coordinate> coordinates)
    {
        var list = coordinates.ToList();
        if (list.Count != Size)
        {
            throw new ArgumentException($"{Name} needs exactly {Size} cells but got {list.Count}.", nameof(coordinates));
        }

        _coordinates.Clear();
        _coordinates.AddRange(list);
        _hits.Clear();
    }

    public bool Occupies(Coordinate coordinate) => _coordinates.Contains(coordinate);

    /// <summary>Registers a hit. Returns false when the cell was already hit or is not part of the ship.</summary>
    public bool RegisterHit(Coordinate coordinate)
    {
        if (!Occupies(coordinate))
        {
            return false;
        }

        return _hits.Add(coordinate);
    }

    public static IReadOnlyList<Ship> CreateGolfBag() => new List<Ship>
    {
        new("Driver", 5),
        new("Fairway Wood", 4),
        new("Hybrid", 3),
        new("Iron", 3),
        new("Putter", 2)
    };
}
