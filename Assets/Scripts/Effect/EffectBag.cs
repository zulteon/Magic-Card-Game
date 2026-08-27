using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting.FullSerializer;

// ─────────────────────────────────────────────────────────────
//  Szerep: megmondja, hogy az effektnek milyen élettartama van,
//  és eltávolításkor mit kell utána takarítani.
//  NEM azt mondja meg, hogy mit csinál — azt továbbra is az
//  Effect.Type + EffectCommands.Registry dönti el.
// ─────────────────────────────────────────────────────────────

public enum EffectRole
{
    Trigger,   // eseményre iratkozik fel (OnTurnEnd stb.)
    Guard,     // bejövő hatást fog el, tölettel
    Aura,      // folyamatosan módosít, amíg él
    Delayed    // N esemény múlva sül el
}

public enum RemoveReason
{
    Death,          // a lény meghalt
    Silence,        // elhallgattatás
    ReturnToHand,    // visszakerült a kézbe
    ZoneChange
}


/// <summary>
/// Egy képesség futásidejű PÉLDÁNYA. A definíció (mit csinál, mit blokkol,
/// milyen ritmusban) az EffectManager-ben van, ide csak az ÁLLAPOT kerül:
/// mennyi töltet maradt, hányszor látta, mikor jár le.
/// Ezért nem tehető az Effect-be: két azonos kártya példánya nem oszthat
/// közös charges mezőt.
/// </summary>

public class LiveEffect
{
    // ===== ÁLLAPOT (ez megy a hálón) =====

    /// <summary>Egyedi fogantyú ehhez a példányhoz. CSAK a szerver osztja!</summary>
    public uint instanceId;

    /// <summary>Az EffectManager-beli definíció azonosítója.</summary>
    public ushort effectId;

    /// <summary>Ki adta ezt az effektet (silence / visszavonás alapja).</summary>
    public ushort sourceId;

    /// <summary>Hányszor tud még elsülni. -1 = korlátlan, 0 = elfogyott.</summary>
    public int charges = 1;

    /// <summary>Hányszor jött szóba eddig (az "every" ritmushoz).</summary>
    public int seen;

    /// <summary>Melyik kör elején jár le. -1 = nem jár le.</summary>
    

    public int howOften=1;

    public int remainingTurns = -1;   // -1 = sosem jár le

    public bool IsExpired => remainingTurns == 0;
    public ushort watchedId;
    public Effect.Type toBlock = Effect.Type.none;

    // ===== DEFINÍCIÓ (lazy, sosem szerializálódik) =====

    [NonSerialized] private Effect _def;

    /// <summary>Első hozzáféréskor töltődik be az EffectManager-ből.</summary>
    public Effect Def => _def ??= EffectManagerClient.instance.GetEffectData(effectId);

    public EffectRole Role;

    public bool IsSpent => charges == 0;

    


    /// <summary>
    /// Megpróbálja elhasználni egy tölettel.
    /// false = most nem jogosult (nem jött el a ritmus szerinti alkalom),
    /// true  = elsül; a hívó dolga eldönteni, mi történjen ezután.
    /// A törlést NEM ez végzi — lásd EffectBag.
    /// </summary>
    public bool TryConsume()
    {
        seen++;

        int every = howOften;
        if (every > 1 && seen % every != 0) return false;

        if (charges > 0) charges--;   // a -1 (korlátlan) sosem fogy
        return true;
    }
}


/// <summary>
/// Egy lény összes élő képessége. Minden MinionLogic-nak saját példánya van.
/// Ez az EGYETLEN kapu: hozzáadni csak Add()-del, törölni csak az itteni
/// metódusokkal lehet — kívülről a lista csak olvasható.
/// </summary>
public class EffectBag
{
    private readonly List<LiveEffect> _list = new(); // nagyon sok effectnél érdemes 3 listára szedni
    private readonly ushort _ownerId;
    public ushort OwnerId => _ownerId;
    
    #region Lock
    public MinionState LockedState { get; private set; }
    public bool LockedIsAlly { get; private set; }
    private bool _locked;
    private int _lockedTurnsRemaining;

    public bool IsLocked => _locked;

    public void Lock(int turns, MinionState state, bool isAlly)
    {
        _locked = true;
        _lockedTurnsRemaining = turns;
        LockedState = state;
        LockedIsAlly = isAlly;
    }
    
    /// <summary>Egy körrel csökkenti a hátralévő zárolást. Ha elérte a 0-t, feloldja és true-t ad.</summary>
    public bool TickLock()
    {
        if (!_locked) return false;

        _lockedTurnsRemaining--;
        if (_lockedTurnsRemaining > 0) return false;

        _locked = false;
        return true;
    }
    #endregion
    public EffectBag(ushort ownerId) => _ownerId = ownerId;

    /// <summary>Olvasható nézet. Add/Remove nincs rajta — szándékosan.</summary>
    public IReadOnlyList<LiveEffect> All => _list;

    public int Count => _list.Count;


    // ═════════ HOZZÁADÁS ═════════

    /// <summary>
    /// Létrehozza a definíció futásidejű példányát és felveszi a listára.
    /// Feliratkozni NEM itt kell — az vezérlés, nem állapot (és a kliens
    /// oldali visszatöltésnél nem szabad megtörténnie).
    /// </summary>
    /*
     Vagyis három réteg, ebben a sorrendben:

idő — FilterByTrigger(effects, time) → kit érdekel ez az esemény egyáltalán // onplay, onsummoned, etc.
scope — self / ally / enemy → engem érint-e ez a konkrét esemény 
feltétel — IfSoTrigger → teljesül-e a szám-feltétel
    */
    public LiveEffect Add(Effect def, ushort sourceId, EffectRole role,
                      int charges = 1, int howOften = 1,
                      int expiresInTurns = -1,          // ÁTNEVEZVE
                      Effect.Type toBlock = Effect.Type.none)
    {
        if (howOften == -1) howOften = 1;

        var live = new LiveEffect
        {
            effectId = def.effectId,
            sourceId = sourceId,
            Role = role,
            toBlock = toBlock,
            charges = charges,
            howOften = howOften,
            remainingTurns = expiresInTurns
        };

        _list.Add(live);
        if (role == EffectRole.Guard) guardCount++;
        return live;
    }

    public bool Has(Effect.Type type)
    {
        if (_locked) return false;
        for (int i = 0; i < _list.Count; i++)
        {
            var e = _list[i];
            if (e.Def != null && e.Def.type == type && !e.IsSpent) return true;
        }
        return false;
    }
    // ═════════ ELFOGÁS (Guard) ═════════

    /// <summary>
    /// Megnézi, van-e olyan élő guard, ami elfogja az adott típusú hatást.
    /// Ha igen, elhasznál belőle egy töltetet és true-val tér vissza.
    /// A beszúrási sorrend a prioritás — determinisztikus, ellentétben
    /// az event-feliratkozók sorrendjével.
    /// </summary>
    private int guardCount;

    public bool TryConsumeGuard(Effect.Type kind)
    {
        if (_locked) return false;
        if (guardCount == 0) return false;

        for (int i = 0; i < _list.Count; i++)
        {
            var e = _list[i];
            if (e.Role != EffectRole.Guard || e.IsSpent) continue;
            if ( e.toBlock != kind) continue;  // none = joker

            if (!e.TryConsume()) continue;

            if (e.IsSpent) RemoveAt(i);
            return true;
        }
        return false;
    }
    public List<Effect> ConsumeByTrigger(Trigger.time when, Effect.Type activity)
    {
        if (_locked) return new List<Effect>();
        var result = new List<Effect>();
        for (int i = _list.Count - 1; i >= 0; i--)
        {

            var e = _list[i];
            if (e.Def == null || e.IsSpent) continue;
            if (e.Def.triggers == null || e.Def.triggers.Length == 0) continue;

            var t = e.Def.triggers[0];
            if (t.t != when || t.activity != activity) continue;

            if (!e.TryConsume()) continue;
            result.Add(e.Def);
            if (e.IsSpent) RemoveAt(i);
        }
        return result;
    }
    /// <summary>Ki védi ezt a lényt? 0 = senki.</summary>
    public ushort GetProtector()
    {
        if (_locked) return 0;
        for (int i = 0; i < _list.Count; i++)
        {
            var e = _list[i];
            if (e.Def != null && e.Def.type == Effect.Type.bodyguard && !e.IsSpent)
            {
                UnityEngine.Debug.Log("Találtunk protectort " + e.Def.effectId);
                return e.sourceId;
                
            }
        }
        return 0;
    }
    public bool ConsumeSleep()
    {
        if ( _locked) return false;
        for (int i = 0; i < _list.Count; i++)
        {
            var e = _list[i];
            if (e.Def == null || e.Def.type != Effect.Type.sleep) continue;
            if (e.IsSpent) continue;

            if (!e.TryConsume()) continue;

            if (e.IsSpent) RemoveAt(i);
            UnityEngine.Debug.Log("CONSUMING SLEEP");
            return true;
        }
        return false;
    }
    // ═════════ TÖRLÉS ═════════

    /// <summary>Visszavonja mindazt, amit egy adott forrás adott (silence).</summary>
    public void RemoveBySource(ushort sourceId)
    {
        for (int i = _list.Count - 1; i >= 0; i--)
            if (_list[i].sourceId == sourceId)
                RemoveAt(i);              // <- nem _list.RemoveAt
    }

    public void TickExpiry()
    {
        if (_locked) return;

        for (int i = _list.Count - 1; i >= 0; i--)
        {
            var e = _list[i];
            if (e.remainingTurns < 0) continue;   // -1 = sosem jár le

            e.remainingTurns--;
            if (e.remainingTurns <= 0)
                RemoveAt(i);
        }
    }


    /// <summary>
    /// A lény kikerült a pályáról. EGYETLEN belépési pont a takarításhoz —
    /// a halál-útvonal, a silence és a kézbe-visszaküldés is ezt hívja.
    /// </summary>
    public void DisposeAll(RemoveReason reason)
    {
        // Feliratkozások: a GameEvents jelenleg minion-szinten tartja nyilván,
        // ezért egyetlen hívás törli az összes triggert.
        GameEvents.Instance.ClearMinion(_ownerId);

        // Késleltetett effektek: halál után a már elhelyezett bomba felrobban,
        // elhallgattatásnál viszont eltűnik.
        if (reason != RemoveReason.Death)
            GameEvents.Instance.CancelDelayed(_ownerId);

        // Guard / Aura: a listával együtt megszűnik, nincs külön teendő.
        _list.Clear();
        guardCount = 0;
    }
    private void RemoveAt(int i)
    {
        var e = _list[i];
        if (e.Role == EffectRole.Guard) guardCount--;
        _list.RemoveAt(i);

        // ha ez volt az utolsó taunt, a state-et is frissíteni kell
        if (e.Def != null && e.Def.type == Effect.Type.taunt)
        {
            bool stillHasTaunt = false;
            foreach (var live in _list)
                if (live.Def != null && live.Def.type == Effect.Type.taunt) { stillHasTaunt = true; break; }

            if (!stillHasTaunt)
                GameManager.instance.ChangeMinionById(_ownerId, s => { s.taunt = false; return s; });
        }
    }

}