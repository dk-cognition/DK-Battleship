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
    private readonly AiSkill _skill;
    private readonly HashSet<Coordinate> _shotsTaken = new();

    /// <summary>Sizes of the clubs still afloat, used by density hunting.</summary>
    private readonly List<int> _remainingSizes = new();

    /// <summary>Cells handed out by <see cref="NextShot"/> whose result has not been reported yet.</summary>
    private readonly HashSet<Coordinate> _pendingShots = new();

    /// <summary>Hits that have not yet been attributed to a sunk club.</summary>
    private readonly HashSet<Coordinate> _openHits = new();

    private readonly List<Coordinate> _targetQueue = new();

    public HuntTargetAi(string name = "Hunt & Target", Random? random = null, bool useParity = true)
        : this(name, random, new AiSkill(UseDensityHunt: false, UseParity: useParity, MistakeChance: 0.0))
    {
    }

    public HuntTargetAi(string name, Random? random, AiSkill skill)
    {
        Name = name;
        _random = random ?? new Random();
        _skill = skill;
        _remainingSizes.AddRange(Ship.CreateGolfBag().Select(s => s.Size));
    }

    public string Name { get; }

    public AiMode Mode => _targetQueue.Count > 0 ? AiMode.Target : AiMode.Hunt;

    public IReadOnlyCollection<Coordinate> TargetQueue => _targetQueue;

    public IReadOnlyCollection<Coordinate> ShotsTaken => _shotsTaken;

    public Coordinate NextShot(BoardView view)
    {
        PruneQueue(view);

        var sloppy = _skill.MistakeChance > 0 && _random.NextDouble() < _skill.MistakeChance;

        if (!sloppy && _targetQueue.Count > 0)
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

        var shot = sloppy ? untried[_random.Next(untried.Count)] : PickHuntCell(view, untried);
        _pendingShots.Add(shot);
        return shot;
    }

    public void RecordResult(Coordinate shot, ShotResult result, int sunkClubSize = 0)
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
                ResolveSunkShip(shot, sunkClubSize);
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
        _remainingSizes.Clear();
        _remainingSizes.AddRange(Ship.CreateGolfBag().Select(s => s.Size));
    }

    private bool IsAvailable(Coordinate c) => !_shotsTaken.Contains(c) && !_pendingShots.Contains(c);

    private Coordinate PickHuntCell(BoardView view, List<Coordinate> untried)
    {
        if (_skill.UseDensityHunt)
        {
            var density = PlacementDensity(view, untried);
            var best = density.Values.Max();
            if (best > 0)
            {
                var contenders = density.Where(kv => kv.Value == best).Select(kv => kv.Key).ToList();
                return contenders[_random.Next(contenders.Count)];
            }
        }

        if (_skill.UseParity)
        {
            var shortest = _remainingSizes.Count > 0 ? _remainingSizes.Min() : 2;
            var parityCells = untried.Where(c => (c.Row + c.Col) % shortest == 0).ToList();
            if (parityCells.Count > 0)
            {
                untried = parityCells;
            }
        }

        return untried[_random.Next(untried.Count)];
    }

    /// <summary>
    /// For every club still in the bag, count the legal placements covering each candidate cell.
    /// Placements crossing a known hit score extra, which merges hunting and targeting.
    /// </summary>
    private Dictionary<Coordinate, int> PlacementDensity(BoardView view, List<Coordinate> untried)
    {
        var density = untried.ToDictionary(c => c, _ => 0);

        foreach (var size in _remainingSizes)
        {
            foreach (var origin in view.AllCells())
            {
                foreach (var step in new[] { (0, 1), (1, 0) })
                {
                    var span = new List<Coordinate>(size);
                    for (var i = 0; i < size; i++)
                    {
                        span.Add(origin.Offset(step.Item1 * i, step.Item2 * i));
                    }

                    if (span.Any(c => !view.InBounds(c) || (view.WasShot(c) && !view.WasHit(c))))
                    {
                        continue;
                    }

                    var weight = 1 + span.Count(c => _openHits.Contains(c)) * 12;
                    foreach (var cell in span)
                    {
                        if (density.ContainsKey(cell))
                        {
                            density[cell] += weight;
                        }
                    }
                }
            }
        }

        return density;
    }

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
    private void ResolveSunkShip(Coordinate lastHit, int sunkClubSize)
    {
        var horizontalRun = CollectRun(lastHit, 0, 1);
        var verticalRun = CollectRun(lastHit, 1, 0);
        var horizontal = WindowForSunkClub(horizontalRun, lastHit, sunkClubSize);
        var vertical = WindowForSunkClub(verticalRun, lastHit, sunkClubSize);

        List<Coordinate> sunkCells;
        if (horizontal is null || vertical is null)
        {
            sunkCells = horizontal ?? vertical ?? new List<Coordinate> { lastHit };
        }
        else if (horizontalRun.Count != verticalRun.Count)
        {
            // The axis whose run the club fills exactly is the club; the longer run spills into a
            // neighbour's hits.
            sunkCells = horizontalRun.Count == sunkClubSize ? horizontal
                : verticalRun.Count == sunkClubSize ? vertical
                : horizontalRun.Count > verticalRun.Count ? horizontal : vertical;
        }
        else
        {
            // Both axes could be the club that just dropped (clubs crossing at this cell). Resolving the
            // wrong one throws away a live lead, so resolve only what they agree on and keep hunting the
            // rest — a few wasted swings beats forgetting a wounded club.
            sunkCells = horizontal.Intersect(vertical).ToList();
        }

        foreach (var cell in sunkCells)
        {
            _openHits.Remove(cell);
        }

        _remainingSizes.Remove(sunkClubSize > 0 ? sunkClubSize : sunkCells.Count);
    }

    /// <summary>
    /// Hits run straight through clubs lying end to end, so the run of hits can be longer than the club
    /// that just dropped. Returns the window of exactly <paramref name="sunkClubSize"/> cells covering
    /// the last swing, preferring one that butts against the end of the run (a sunk club's ends touch
    /// water or the board edge, never another club's hits), or null when this axis cannot hold the club.
    /// </summary>
    private static List<Coordinate>? WindowForSunkClub(List<Coordinate> run, Coordinate lastHit, int sunkClubSize)
    {
        if (sunkClubSize <= 0)
        {
            return run;
        }

        if (sunkClubSize > run.Count)
        {
            return null;
        }

        var lastHitIndex = run.IndexOf(lastHit);
        var chosen = Enumerable
            .Range(0, run.Count - sunkClubSize + 1)
            .Where(start => start <= lastHitIndex && lastHitIndex < start + sunkClubSize)
            .OrderBy(start => (start > 0 ? 1 : 0) + (start + sunkClubSize < run.Count ? 1 : 0))
            .First();

        return run.GetRange(chosen, sunkClubSize);
    }

    /// <summary>Open hits in a straight line through <paramref name="origin"/>, ordered along the axis.</summary>
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

        return run.OrderBy(c => c.Row).ThenBy(c => c.Col).ToList();
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
