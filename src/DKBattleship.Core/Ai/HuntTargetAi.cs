namespace DKBattleship.Core.Ai;

public enum AiMode
{
    Hunt,
    Target
}

/// <summary>
/// Classic hunt/target strategy: sweep the course at random until a club is struck, then work the
/// adjacent cells until it is in the hole, then go back to sweeping.
/// </summary>
public class HuntTargetAi : IAiPlayer
{
    private readonly Random _random;
    private readonly bool _useParity;
    private readonly HashSet<Coordinate> _shotsTaken = new();

    /// <summary>Cells handed out by <see cref="NextShot"/> whose result has not been reported yet.</summary>
    private readonly HashSet<Coordinate> _pendingShots = new();

    /// <summary>Hits that have not yet been attributed to a sunk club.</summary>
    private readonly HashSet<Coordinate> _openHits = new();

    private readonly List<Coordinate> _targetQueue = new();

    public HuntTargetAi(string name = "Hunt & Target", Random? random = null, bool useParity = true)
    {
        Name = name;
        _random = random ?? new Random();
        _useParity = useParity;
    }

    public string Name { get; }

    public AiMode Mode => _targetQueue.Count > 0 ? AiMode.Target : AiMode.Hunt;

    public IReadOnlyCollection<Coordinate> TargetQueue => _targetQueue;

    public IReadOnlyCollection<Coordinate> ShotsTaken => _shotsTaken;

    public Coordinate NextShot(BoardView view)
    {
        PruneQueue(view);

        if (_targetQueue.Count > 0)
        {
            var target = _targetQueue[0];
            _targetQueue.RemoveAt(0);
            _pendingShots.Add(target);
            return target;
        }

        var untried = view.UntriedCells().Where(IsAvailable).ToList();
        if (untried.Count == 0)
        {
            throw new InvalidOperationException("No cells left to swing at.");
        }

        if (_useParity)
        {
            var parityCells = untried.Where(c => (c.Row + c.Col) % 2 == 0).ToList();
            if (parityCells.Count > 0)
            {
                untried = parityCells;
            }
        }

        var shot = untried[_random.Next(untried.Count)];
        _pendingShots.Add(shot);
        return shot;
    }

    public void RecordResult(Coordinate shot, ShotResult result)
    {
        _shotsTaken.Add(shot);
        _pendingShots.Remove(shot);
        _targetQueue.Remove(shot);

        switch (result)
        {
            case ShotResult.Hit:
                _openHits.Add(shot);
                EnqueueNeighbours(shot);
                break;
            case ShotResult.Sunk:
                _openHits.Add(shot);
                ResolveSunkShip(shot);
                RebuildQueueFromOpenHits();
                break;
        }
    }

    public void Reset()
    {
        _shotsTaken.Clear();
        _pendingShots.Clear();
        _openHits.Clear();
        _targetQueue.Clear();
    }

    private bool IsAvailable(Coordinate c) => !_shotsTaken.Contains(c) && !_pendingShots.Contains(c);

    private void EnqueueNeighbours(Coordinate origin)
    {
        foreach (var neighbour in origin.Neighbours())
        {
            if (IsAvailable(neighbour) && !_targetQueue.Contains(neighbour))
            {
                _targetQueue.Add(neighbour);
            }
        }
    }

    /// <summary>
    /// A sunk club is the straight run of hits through <paramref name="lastHit"/>; those cells stop
    /// being leads, while hits belonging to another club stay open.
    /// </summary>
    private void ResolveSunkShip(Coordinate lastHit)
    {
        var horizontal = CollectRun(lastHit, 0, 1);
        var vertical = CollectRun(lastHit, 1, 0);
        var sunkCells = horizontal.Count >= vertical.Count ? horizontal : vertical;

        foreach (var cell in sunkCells)
        {
            _openHits.Remove(cell);
        }
    }

    private List<Coordinate> CollectRun(Coordinate origin, int rowStep, int colStep)
    {
        var run = new List<Coordinate> { origin };

        foreach (var sign in new[] { 1, -1 })
        {
            var cursor = origin.Offset(rowStep * sign, colStep * sign);
            while (_openHits.Contains(cursor))
            {
                run.Add(cursor);
                cursor = cursor.Offset(rowStep * sign, colStep * sign);
            }
        }

        return run;
    }

    private void RebuildQueueFromOpenHits()
    {
        _targetQueue.Clear();
        foreach (var hit in _openHits)
        {
            EnqueueNeighbours(hit);
        }
    }

    private void PruneQueue(BoardView view) =>
        _targetQueue.RemoveAll(c => !view.InBounds(c) || view.WasShot(c) || !IsAvailable(c));
}
