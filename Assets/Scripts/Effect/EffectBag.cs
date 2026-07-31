using System;
using System.Collections.Generic;

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
    public int expiresOnTurn = -1;

    public int howOften=1;

    public Effect.Type toBlock = Effect.Type.none;

    // ===== DEFINÍCIÓ (lazy, sosem szerializálódik) =====

    [NonSerialized] private Effect _def;

    /// <summary>Első hozzáféréskor töltődik be az EffectManager-ből.</summary>
    public Effect Def => _def ??= EffectManagerClient.instance.GetEffectData(effectId);

    public EffectRole Role;

    public bool IsSpent => charges == 0;

    public bool IsExpired(int currentTurn)
        => expiresOnTurn >= 0 && currentTurn >= expiresOnTurn;


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
    public LiveEffect Add(Effect def, ushort sourceId,
                      EffectRole role,
                      int charges = 1, int howOften = 1,
                      int expiresOnTurn = -1,
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
            expiresOnTurn = expiresOnTurn
        };

        _list.Add(live);
        if (role == EffectRole.Guard) guardCount++;
        return live;
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


    // ═════════ TÖRLÉS ═════════

    /// <summary>Visszavonja mindazt, amit egy adott forrás adott (silence).</summary>
    public void RemoveBySource(ushort sourceId)
    {
        for (int i = _list.Count - 1; i >= 0; i--)
            if (_list[i].sourceId == sourceId)
                RemoveAt(i);              // <- nem _list.RemoveAt
    }

    public void TickExpiry(int currentTurn)
    {
        for (int i = _list.Count - 1; i >= 0; i--)
            if (_list[i].IsExpired(currentTurn))
                RemoveAt(i);              // <- ugyanígy
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
        if (_list[i].Role == EffectRole.Guard) guardCount--;
        _list.RemoveAt(i);
    }
}