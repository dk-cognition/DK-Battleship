namespace DKBattleship.Core;

/// <summary>Zero-based position on a board.</summary>
public readonly record struct Coordinate(int Row, int Col)
{
    public Coordinate Offset(int rowDelta, int colDelta) => new(Row + rowDelta, Col + colDelta);

    /// <summary>Orthogonal neighbours, without any bounds filtering.</summary>
    public IEnumerable<Coordinate> Neighbours()
    {
        yield return Offset(-1, 0);
        yield return Offset(1, 0);
        yield return Offset(0, -1);
        yield return Offset(0, 1);
    }

    /// <summary>Golf-scorecard style label, e.g. (0,0) => "A1".</summary>
    public override string ToString() => $"{(char)('A' + Col)}{Row + 1}";
}
