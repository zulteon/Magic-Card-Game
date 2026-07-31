using System.Collections.Generic;

/* ════════════════════════════════════════════════════════════════════
   TODO — ami még hátra van

   [ ] DeadCards.Add: nálad KI VAN KOMMENTELVE → a sírkert üres marad,
       tehát a feltámasztós/keresős lapok nem fognak működni.

   [ ] ifso blokk: egyelőre kikommentelve

   [ ] RaiseMinionDied: ushort-os szignatúra kell a GameEvents-ben
       (a MinionLogic ekkor már nincs a listában)

   [ ] GraveEntry (cardId + attack/health): az Axolotl és a Kóbor kutya
       ("felezett statját kapja") igényelni fogja. Addig elég a cardId.

   [ ] Deathrattle-sorrend: a wave a jelölés sorrendjét követi. Ha determinisztikus
       szabály kell (HS-ben a kijátszás sorrendje dönt), a wave-et rendezni kell.
    //A RaiseMinionDied továbbra is kommentben van, mert a GameEvents-ben MinionLogic
   ════════════════════════════════════════════════════════════════════ */

public class Graveyard
{
    /// <summary>Halálra ítéltek, akiket még nem takarítottunk el.</summary>
    private readonly List<ushort> _pendingDeaths = new List<ushort>();
    private bool _executing;

    /// <summary>A sírkert: cardId-k, mert a feltámasztásnak azt kell tudnia, MIT idézzen.</summary>
    public List<ushort> DeadCards { get; private set; } = new List<ushort>();

    /// <summary>Csak felírja a listára az áldozatot. Nem öl, nem takarít.</summary>
    public void SendToGraveyard(ushort id)
    {
        if (!_pendingDeaths.Contains(id))
            _pendingDeaths.Add(id);
    }

    /// <summary>
    /// Végrehajtja a halál folyamatát — HULLÁMOKBAN, nem egyesével.
    /// Az egyszerre haló lények így nem kapják meg egymás halálhörgés-buffját:
    /// mire az első deathrattle elsül, mindenki lekerült már a pályáról.
    /// </summary>
    public void Execute()
    {
        if (_executing)
            return;   // a hullám-ciklus már fut; az új halottak a következő hullámba esnek

        _executing = true;
        int safety = 0;

        try
        {
            while (_pendingDeaths.Count > 0)
            {
                if (++safety > 50)
                {
                    UnityEngine.Debug.LogError("Graveyard: végtelen halál-lánc, megszakítva.");
                    _pendingDeaths.Clear();
                    return;
                }

                // Az AKTUÁLIS hullám kimásolása, hogy a lista közben szabadon töltődhessen
                var wave = new List<ushort>(_pendingDeaths);
                _pendingDeaths.Clear();

                // MINDEN halott ide kerül, nem csak akinek van deathrattle-je —
                // különben a "ha meghal egy lény" típusú triggerek (Shoebill)
                // nem értesülnének a deathrattle nélküli halálokról.
                var dead = new List<(ushort id, List<Effect> effects)>();

                // ── 1. FÁZIS: mindenki EGYSZERRE hal meg ──────────────────
                // Se esemény, se effekt nem fut itt — csak pillanatkép és levétel.
                foreach (ushort id in wave)
                {
                    MinionLogic minion = GameManager.instance.GetMinionLogic(id);
                    if (minion == null) continue;   // már eltűnt (pl. kézbe került)

                    // ÚJRAELLENŐRZÉS: a jelölés óta meggyógyulhatott
                    // (pl. egy korábbi hullám deathrattle-je). Ilyenkor túlél.
                    //if (minion.Health > 0) continue;
                     
                    // A pillanatkép a deathrattle ELŐTT készül: a saját halála által
                    // adott buff már nem érvényes rá.
                    DeadCards.Add(minion.cardId);

                    var deathEffects = TriggerChecker.instance.GetOnDeathEffect(id);

                    GameManager.instance.RemoveFromBoard(id);

                    dead.Add((id, deathEffects));
                }

                if (dead.Count == 0) continue;

                // ── KÖZÖS HALÁLANIMÁCIÓ: egy ClientEvent az egész hullámra ──
                var deadIds = new ushort[dead.Count];
                for (int i = 0; i < dead.Count; i++)
                    deadIds[i] = dead[i].id;

                GameManager.instance.RecieveEvent(new ClientEvent
                {
                    effectType = (ushort)Effect.Type.death,
                    targetIds = deadIds,
                    doerId = deadIds[0]
                });

                // ── 2. FÁZIS: csak most jönnek az események és a halálhörgések ──
                // Ekkor már mindenki lekerült a pályáról, tehát a buffok
                // CSAK A TÚLÉLŐKET érhetik el.
                foreach (var (id, effects) in dead)
                {
                     //GameEvents.Instance.RaiseMinionDied(id);

                    if (effects.Count == 0) continue;

                    // ifso — egyelőre single, ha több lesz, könnyen bővíthető
                    /*
                    List<Effect> ifsoeffects = TriggerChecker.instance.CheckTrigger(Trigger.time.ifso, id);
                    foreach (var item in ifsoeffects)
                    {
                        Trigger chosen = null;
                        foreach (Trigger i in item.triggers)
                            if (i.t == Trigger.time.ifso) { chosen = i; break; }

                        if (!TriggerChecker.instance.IfSoTrigger(chosen, GameManager.instance.GetMinionLogic(id), null))
                            effects.Remove(item);
                    }
                    */

                    GameManager.instance.DoEffects(effects.ToArray(), id, null);
                }

                // Ha ezek újabb halált okoztak, azok már a KÖVETKEZŐ hullámban vannak.
            }
        }
        finally { _executing = false; }
    }
}