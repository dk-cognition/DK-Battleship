namespace DKBattleship.Core;

using DKBattleship.Core.Ai;

/// <summary>Result of a single AI swing, for the UI to narrate.</summary>
public readonly record struct AiShot(Coordinate Coordinate, ShotResult Result, string? ShipName);

/// <summary>A full round of golf-themed battleship: the player's course, the opponent's course and turn order.</summary>
public class Game
{
    private readonly Random _random;
    private readonly List<Ship> _shipsToPlace;

    public Game(GolfCharacter? opponent = null, int rows = Board.DefaultSize, int cols = Board.DefaultSize, Random? random = null)
    {
        _random = random ?? new Random();
        Opponent = opponent ?? GolfCharacters.TigerWoods;
        Ai = Opponent.CreateStrategy(_random);
        PlayerBoard = new Board(rows, cols);
        AiBoard = new Board(rows, cols);
        _shipsToPlace = Ship.CreateGolfBag().ToList();
        ShipPlacer.PlaceRandomly(AiBoard, Ship.CreateGolfBag(), _random);
        Status = GameStatus.Placing;
        StatusMessage = $"Load the bag: place your clubs, then tee off against {Opponent.Name}.";
    }

    public Board PlayerBoard { get; }

    public Board AiBoard { get; }

    public GolfCharacter Opponent { get; }

    public IAiPlayer Ai { get; }

    public GameStatus Status { get; private set; }

    public string StatusMessage { get; private set; }

    /// <summary>Clubs the player still has to place, in order.</summary>
    public IReadOnlyList<Ship> ShipsToPlace => _shipsToPlace;

    public Ship? NextShipToPlace => _shipsToPlace.FirstOrDefault();

    public bool IsOver => Status is GameStatus.PlayerWon or GameStatus.AiWon;

    public int PlayerSwings { get; private set; }

    public int AiSwings { get; private set; }

    /// <summary>Places the next club for the player. Returns false when the lie is invalid.</summary>
    public bool PlacePlayerShip(Coordinate start, Orientation orientation)
    {
        if (Status != GameStatus.Placing)
        {
            return false;
        }

        var ship = NextShipToPlace;
        if (ship is null)
        {
            return false;
        }

        if (!PlayerBoard.PlaceShip(ship, start, orientation))
        {
            StatusMessage = $"No good lie there for the {ship.Name}. Try another spot.";
            return false;
        }

        _shipsToPlace.RemoveAt(0);
        StatusMessage = _shipsToPlace.Count > 0
            ? $"{ship.Name} is in the bag. Next up: {NextShipToPlace!.Name} ({NextShipToPlace.Size} cells)."
            : "Bag is loaded. Time to tee off!";

        if (_shipsToPlace.Count == 0)
        {
            StartBattle();
        }

        return true;
    }

    public void RandomizePlayerShips()
    {
        if (Status != GameStatus.Placing)
        {
            return;
        }

        ShipPlacer.PlaceRandomly(PlayerBoard, _shipsToPlace.ToList(), _random);
        _shipsToPlace.Clear();
        StatusMessage = "Caddie loaded the bag for you. Time to tee off!";
        StartBattle();
    }

    private void StartBattle()
    {
        Status = GameStatus.PlayerTurn;
        StatusMessage = $"You're on the tee against {Opponent.Name}. Pick a cell on their course.";
    }

    /// <summary>Player swings at the opponent's course. Turn passes to the AI unless the shot was wasted.</summary>
    public ShotResult PlayerFire(Coordinate coordinate)
    {
        if (Status != GameStatus.PlayerTurn)
        {
            return ShotResult.AlreadyShot;
        }

        var ship = AiBoard.ShipAt(coordinate);
        var result = AiBoard.ReceiveShot(coordinate);
        if (result == ShotResult.AlreadyShot)
        {
            StatusMessage = $"You already played {coordinate}. Pick a fresh cell.";
            return result;
        }

        PlayerSwings++;
        StatusMessage = result switch
        {
            ShotResult.Miss => $"{coordinate}: sliced it into the rough. Miss.",
            ShotResult.Hit => $"{coordinate}: solid contact! You clipped a club.",
            ShotResult.Sunk => $"{coordinate}: {ship?.Name} is in the hole!",
            _ => StatusMessage
        };

        if (AiBoard.AllShipsSunk)
        {
            Status = GameStatus.PlayerWon;
            StatusMessage = $"Match over — you cleared {Opponent.Name}'s bag in {PlayerSwings} swings. Winner!";
            return result;
        }

        Status = GameStatus.AiTurn;
        return result;
    }

    /// <summary>AI takes its swing at the player's course. Turn passes back to the player.</summary>
    public AiShot AiFire()
    {
        if (Status != GameStatus.AiTurn)
        {
            throw new InvalidOperationException($"It is not {Opponent.Name}'s turn (status: {Status}).");
        }

        var coordinate = MustHitToStayUnderCap()
            ? PickGuaranteedHit()
            : Ai.NextShot(new BoardView(PlayerBoard));
        var ship = PlayerBoard.ShipAt(coordinate);
        var result = PlayerBoard.ReceiveShot(coordinate);
        Ai.RecordResult(coordinate, result, result == ShotResult.Sunk ? ship!.Size : 0);
        AiSwings++;

        StatusMessage = result switch
        {
            ShotResult.Miss => $"{Opponent.Name} swung at {coordinate} and found the rough.",
            ShotResult.Hit => $"{Opponent.Name} hit your {ship?.Name} at {coordinate}.",
            ShotResult.Sunk => $"{Opponent.Name} put your {ship?.Name} in the hole at {coordinate}!",
            _ => $"{Opponent.Name} wasted a swing at {coordinate}."
        };

        if (PlayerBoard.AllShipsSunk)
        {
            Status = GameStatus.AiWon;
            StatusMessage = $"{Opponent.Name} cleared your bag in {AiSwings} swings. Better luck next round.";
        }
        else
        {
            Status = GameStatus.PlayerTurn;
        }

        return new AiShot(coordinate, result, ship?.Name);
    }

    /// <summary>
    /// True when the opponent's <see cref="GolfCharacter.SwingCap"/> leaves no room for another miss:
    /// there are at least as many club cells left standing as swings left under the cap, so every
    /// remaining swing has to connect.
    /// </summary>
    private bool MustHitToStayUnderCap()
    {
        if (Opponent.SwingCap is not int cap)
        {
            return false;
        }

        var cellsLeft = PlayerBoard.Ships.Sum(s => s.Size - s.Hits.Count);
        return cellsLeft >= cap - AiSwings;
    }

    /// <summary>A certain hit: finishes a wounded club first so the closing run reads naturally.</summary>
    private Coordinate PickGuaranteedHit()
    {
        var club = PlayerBoard.Ships
            .Where(s => !s.IsSunk)
            .OrderByDescending(s => s.Hits.Count > 0)
            .First();
        var cells = club.Coordinates.Where(c => PlayerBoard[c] == CellState.Ship).ToList();
        return cells[_random.Next(cells.Count)];
    }
}
