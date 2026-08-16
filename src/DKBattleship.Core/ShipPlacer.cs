namespace DKBattleship.Core;

/// <summary>Auto-places a bag of clubs at random valid positions.</summary>
public static class ShipPlacer
{
    public static void PlaceRandomly(Board board, IEnumerable<Ship> ships, Random random)
    {
        foreach (var ship in ships)
        {
            if (!TryPlaceRandomly(board, ship, random))
            {
                throw new InvalidOperationException($"Could not find a valid lie for {ship.Name} on this course.");
            }
        }
    }

    public static bool TryPlaceRandomly(Board board, Ship ship, Random random)
    {
        var candidates = new List<(Coordinate Start, Orientation Orientation)>();
        foreach (var orientation in new[] { Orientation.Horizontal, Orientation.Vertical })
        {
            for (var row = 0; row < board.Rows; row++)
            {
                for (var col = 0; col < board.Cols; col++)
                {
                    var start = new Coordinate(row, col);
                    if (board.CanPlace(ship, start, orientation))
                    {
                        candidates.Add((start, orientation));
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        var (chosenStart, chosenOrientation) = candidates[random.Next(candidates.Count)];
        return board.PlaceShip(ship, chosenStart, chosenOrientation);
    }
}
