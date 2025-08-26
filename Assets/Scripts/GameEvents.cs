using System;

public class GameEvents
{
    // Singleton instance (glob�lis el�r�shez)
    public static GameEvents Instance { get; private set; } = new GameEvents();

    // ===== J�T�K ESEM�NYEK =====
    public event Action OnTurnStart;
    public event Action OnTurnEnd;

    // ===== MINION ESEM�NYEK =====
    public event Action<LiveMinion> OnMinionSummoned;
    public event Action<LiveMinion> OnMinionDied;
    public event Action<LiveMinion> OnMinionPlayed;

    // ===== ESEM�NY TRIGGEREK =====
    public void RaiseTurnStart() => OnTurnStart?.Invoke();
    public void RaiseTurnEnd() => OnTurnEnd?.Invoke();

    public void RaiseMinionSummoned(LiveMinion minion) => OnMinionSummoned?.Invoke(minion);
    public void RaiseMinionDied(LiveMinion minion) => OnMinionDied?.Invoke(minion);
    public void RaiseMinionPlayed(LiveMinion minion) => OnMinionPlayed?.Invoke(minion);

    // ===== P�LD�NY CSER�JE (pl. �j meccs) =====
    public static void Reset()
    {
        Instance = new GameEvents();
    }
}
