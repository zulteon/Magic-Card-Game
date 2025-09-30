using System;

public class GameEvents
{
    // Singleton instance
    public static GameEvents Instance { get; private set; } = new GameEvents();

    // ===== JÁTÉK ESEMÉNYEK =====
    public event Action OnTurnStart;
    public event Action OnTurnEnd;

    public event Action<int> OnManaChanged;  // int = új mana érték
    public event Action<int> OnCardDrawn;    // int = cardId

    // ===== MINION ESEMÉNYEK =====
    public event Action<MinionLogic> OnMinionSummoned;
    public event Action<MinionLogic> OnMinionPlayed;
    public event Action<MinionLogic> OnMinionDied;
    public event Action<MinionLogic, int> OnMinionDamaged; // target, dmg
    public event Action<MinionLogic, int> OnMinionHealed;  // target, heal

    // ===== HERO ESEMÉNYEK =====
    public event Action<ushort, int> OnHeroDamaged; // heroId, dmg
    public event Action<ushort, int> OnHeroHealed;  // heroId, heal

    // ===== TRIGGEREK =====
    public void RaiseTurnStart() => OnTurnStart?.Invoke();
    public void RaiseTurnEnd() => OnTurnEnd?.Invoke();

    public void RaiseManaChanged(int mana) => OnManaChanged?.Invoke(mana);
    public void RaiseCardDrawn(int cardId) => OnCardDrawn?.Invoke(cardId);

    public void RaiseMinionSummoned(MinionLogic m) => OnMinionSummoned?.Invoke(m);
    public void RaiseMinionPlayed(MinionLogic m) => OnMinionPlayed?.Invoke(m);
    public void RaiseMinionDied(MinionLogic m) => OnMinionDied?.Invoke(m);
    public void RaiseMinionDamaged(MinionLogic m, int dmg) => OnMinionDamaged?.Invoke(m, dmg);
    public void RaiseMinionHealed(MinionLogic m, int heal) => OnMinionHealed?.Invoke(m, heal);

    public void RaiseHeroDamaged(ushort heroId, int dmg) => OnHeroDamaged?.Invoke(heroId, dmg);
    public void RaiseHeroHealed(ushort heroId, int heal) => OnHeroHealed?.Invoke(heroId, heal);

    // ===== RESET =====
    public static void Reset()
    {
        Instance = new GameEvents();
    }
}
