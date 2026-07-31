using System;
using System.Collections.Generic;
using System.Diagnostics;

public class GameEvents
{
    // Singleton instance

    // GameEvents megfelelõ használata : 
    // 1. Eltároljuk a hivatkozást
    /*Action myAction;

    void Start()
    {
        myAction = () => EffectRunner.Run(someEffect);
        GameEvents.Instance.OnTurnEnd += myAction;
    }

    void OnDestroy()
    {
        // 2. Így már le tudunk iratkozni!
        GameEvents.Instance.OnTurnEnd -= myAction;
    }*/
    //############ uj eseménnyel bövités

    /*  
    Új esemény hozzáadása (3 lépés):

    1. Enum bõvítése: Add hozzá az új típust a public enum EventType { ... } listához.

    2.Esemény és Kürt: * public event Action<Param> OnUjEsemeny;

        public void RaiseUjEsemeny(Param p) => OnUjEsemeny?.Invoke(p);

    Iratkozás/Takarítás: * AddEvent switch-be: case EventType.Uj: OnUjEsemeny += handler; break;

        ClearMinion switch-be: case EventType.Uj: OnUjEsemeny -= sub.Handler; break;
     
     */
    public enum EventType { TurnStart, TurnEnd, ManaChanged }
    public static GameEvents Instance { get; private set; } = new GameEvents();
    // ===== JÁTÉK ESEMÉNYEK =====
    public event Action OnTurnStart;
    public event Action OnTurnEnd;
    public event Action<Action,int> DelayedEffects; // milyen akciora  int =hány körig

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
    // ===== A TAKARÍTÓ RENDSZER =====
    private class Subscription
    {
        public Action Handler;
        public EventType Type;
    }

    #region Delayed effects

    

    private readonly Dictionary<EventType, List<DelayedEntry>> _delayed = new();
    private readonly HashSet<EventType> _ticking = new();

    /// <summary>N darab "clock" esemény után lefuttatja a callbacket.</summary>
    private class DelayedEntry
    {
        // Futásidejû delegate: nem szerializálható, hálón/mentésben nem megy át.
        // Ha a szervernek replikálnia kell a függõ effekteket, ide effectId + paraméterek
        // kerül, és hívás elõtt a registry-bõl oldjuk fel (lásd LiveAura lazy-load).
        public Action callback;
        public int remaining;
        public ushort ownerId;      // 0 = nincs gazdája
    }

    public void AddDelayedEffect(Action callback, int count,
                                 EventType clock = EventType.TurnEnd,
                                 ushort ownerId = 0)
    {
        if (count < 1) { callback?.Invoke(); return; }

        if (!_delayed.TryGetValue(clock, out var list))
            _delayed[clock] = list = new List<DelayedEntry>();

        list.Add(new DelayedEntry { callback = callback, remaining = count, ownerId = ownerId });
    }

    public void CancelDelayed(ushort ownerId)
    {
        foreach (var list in _delayed.Values)
            list.RemoveAll(e => e.ownerId == ownerId);
    }
    private void TickDelayed(EventType clock)
    {
        if (!_delayed.TryGetValue(clock, out var list) || list.Count == 0) return;
        if (!_ticking.Add(clock)) return;

        try
        {
            List<DelayedEntry> due = null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (--list[i].remaining > 0) continue;
                (due ??= new List<DelayedEntry>()).Add(list[i]);
                list.RemoveAt(i);
            }

            if (due == null) return;
            for (int i = due.Count - 1; i >= 0; i--)   // vissza az eredeti sorrendre
                due[i].callback?.Invoke();
        }
        finally { _ticking.Remove(clock); }
    }

    #endregion
    // Itt jegyezzük fel, melyik ID-hoz milyen feliratkozások tartoznak
    private Dictionary<ushort, List<Subscription>> _registry = new Dictionary<ushort, List<Subscription>>();

    // ===== OLVASHATÓ FELIRATKOZÁS (AddEvent) =====
    public void AddEvent(ushort seqId, EventType type, Action handler)
    {
        // A Switch-case sokkal olvashatóbb, mint a lambdák!
        switch (type)
        {
            case EventType.TurnStart: OnTurnStart += handler; break;
            case EventType.TurnEnd: OnTurnEnd += handler; break;
        }

        // Elmentjük a listába a késõbbi törléshez
        if (!_registry.ContainsKey(seqId)) _registry[seqId] = new List<Subscription>();
        _registry[seqId].Add(new Subscription { Handler = handler, Type = type });
    }

    // ===== TÖRLÉS (Silence vagy Halál esetén) =====
    public void ClearMinion(ushort seqId)
    {
        if (!_registry.ContainsKey(seqId)) return;

        foreach (var sub in _registry[seqId])
        {
            // Itt pontosan ugyanúgy iratkozunk le, ahogy feliratkoztunk
            switch (sub.Type)
            {
                case EventType.TurnStart: OnTurnStart -= sub.Handler; break;
                case EventType.TurnEnd: OnTurnEnd -= sub.Handler; break;
            }
        }
        _registry.Remove(seqId);
    }
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
    private class EventSubscription
    {
        public Action Handler; // Maga a metódus, amit meghívunk
        public Action<Action> UnsubscribeAction; // A "takarító" kód (pl. h => OnTurnEnd -= h)
    }

}

public static class TriggerConverter
{
    public static bool ActiveEffectConverter(
        Trigger.time trigger,
        out GameEvents.EventType eventType)
    {
        switch (trigger)
        {
            case Trigger.time.startofturn:
                eventType = GameEvents.EventType.TurnStart;
                return true;

            case Trigger.time.endofturn:
                eventType = GameEvents.EventType.TurnEnd;
                return true;

            default:
                eventType = default;
                return false;
        }
    }
}