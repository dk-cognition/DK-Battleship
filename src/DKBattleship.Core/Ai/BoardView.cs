namespace DKBattleship.Core.Ai;

/// <summary>
/// Fog-of-war view of a board handed to an AI: dimensions and previously revealed cells only,
/// never the location of un-hit ships.
/// </summary>
public sealed class BoardView
{
    private readonly Board _board;

    public BoardView(Board board) => _board = board;

    public int Rows => _board.Rows;

    public int Cols => _board.Cols;

    public bool InBounds(Coordinate c) => _board.InBounds(c);

    public bool WasShot(Coordinate c) => InBounds(c) && _board[c] is CellState.Hit or CellState.Miss;

    public IEnumerable<Coordinate> AllCells()
    {
        for (var row = 0; row < Rows; row++)
        {
            for (var col = 0; col < Cols; col++)
            {
                yield return new Coordinate(row, col);
            }
        }
    }

    public IEnumerable<Coordinate> UntriedCells() => AllCells().Where(c => !WasShot(c));
}
