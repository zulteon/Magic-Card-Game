using System;

public class GameEvents
{
    // Singleton instance (globális eléréshez)
    public static GameEvents Instance { get; private set; } = new GameEvents();

    // ===== JÁTÉK ESEMÉNYEK =====
    public event Action OnTurnStart;
    public event Action OnTurnEnd;

    // ===== MINION ESEMÉNYEK =====
    public event Action<LiveMinion> OnMinionSummoned;
    public event Action<LiveMinion> OnMinionDied;
    public event Action<LiveMinion> OnMinionPlayed;

    // ===== ESEMÉNY TRIGGEREK =====
    public void RaiseTurnStart() => OnTurnStart?.Invoke();
    public void RaiseTurnEnd() => OnTurnEnd?.Invoke();

    public void RaiseMinionSummoned(LiveMinion minion) => OnMinionSummoned?.Invoke(minion);
    public void RaiseMinionDied(LiveMinion minion) => OnMinionDied?.Invoke(minion);
    public void RaiseMinionPlayed(LiveMinion minion) => OnMinionPlayed?.Invoke(minion);

    // ===== PÉLDÁNY CSERÉJE (pl. új meccs) =====
    public static void Reset()
    {
        Instance = new GameEvents();
    }
}
