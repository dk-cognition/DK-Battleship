namespace DKBattleship.Core.Ai;

/// <summary>
/// Strategy for an AI opponent. Future golf characters each supply their own implementation
/// so personality and difficulty stay behind this interface.
/// </summary>
public interface IAiPlayer
{
    string Name { get; }

    /// <summary>Chooses the next cell to swing at. Must never return a cell already shot.</summary>
    Coordinate NextShot(BoardView view);

    /// <summary>
    /// Feeds the outcome of the AI's last shot back into its strategy. When the shot put a club in the
    /// hole, <paramref name="sunkClubSize"/> is that club's size — the same "you sank my Putter" call a
    /// human opponent makes, and what lets the strategy tell one club's hits from its neighbour's.
    /// </summary>
    void RecordResult(Coordinate shot, ShotResult result, int sunkClubSize = 0);

    /// <summary>Clears all state so the same instance can play another round.</summary>
    void Reset();
}
