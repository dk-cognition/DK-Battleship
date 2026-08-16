namespace DKBattleship.Core;

public enum CellState
{
    Empty,
    Ship,
    Hit,
    Miss
}

public enum Orientation
{
    Horizontal,
    Vertical
}

/// <summary>Outcome of a single swing at a board.</summary>
public enum ShotResult
{
    Miss,
    Hit,
    Sunk,
    AlreadyShot
}

public enum GameStatus
{
    Placing,
    PlayerTurn,
    AiTurn,
    PlayerWon,
    AiWon
}
