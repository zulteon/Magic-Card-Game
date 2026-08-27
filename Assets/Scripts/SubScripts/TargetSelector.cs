using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EGYETLEN kliensoldali célzó-mód. Nem tudja, mire használják —
/// kap egy érvényes listát és egy callbacket, és visszaadja a választást.
/// Így a támadás és a battlecry nem tud egymásba akadni.
/// </summary>
public class TargetSelector : MonoBehaviour
{
    public static TargetSelector instance;

    private List<ushort> _valid;
    private Action<ushort> _onPicked;
    private bool _active;

    public bool IsActive => _active;

    private void Awake() => instance = this;

    /// <summary>
    /// Célzó-mód indítása. A callback a KIVÁLASZTOTT id-t kapja.
    /// Csak ready fázisban indul — animáció közben nem lehet célozni.
    /// </summary>
    public void Begin(List<ushort> validTargets, Vector3 arrowFrom, Action<ushort> onPicked)
    {
        if (GameManager.instance.phase != GameManager.Phase.ready)
        {
            Debug.Log("[TargetSelector] Most nem lehet célozni (fázis: "
                      + GameManager.instance.phase + ")");
            return;
        }

        if (validTargets == null || validTargets.Count == 0)
        {
            Debug.LogWarning("[TargetSelector] Nincs érvényes célpont.");
            return;
        }

        Cancel();   // ha épp futna egy másik célzás, azt lezárjuk
        foreach (var target in validTargets)
        {
            print(target.ToString());
        }
        _valid = validTargets;
        _onPicked = onPicked;

        GameManager.instance.phase = GameManager.Phase.targeting;

        Arrow3DPointer.instance.SetArrow(arrowFrom);
        HighlightAll(true);

        // Egy képkocka késleltetés, hogy az INDÍTÓ kattintás ne zárja le azonnal
        StartCoroutine(ActivateNextFrame());
    }

    private IEnumerator ActivateNextFrame()
    {
        yield return null;
        _active = true;
    }

    public void Cancel()
    {
        if (!_active && _valid == null) return;

        _active = false;
        HighlightAll(false);
        _valid = null;
        _onPicked = null;

        if (Arrow3DPointer.instance != null)
            Arrow3DPointer.instance.TurnOff();

        // Csak akkor állítjuk vissza, ha tényleg mi állítottuk targeting-re.
        // (Ha közben animation lett, azt nem írjuk felül.)
        if (GameManager.instance != null &&
            GameManager.instance.phase == GameManager.Phase.targeting)
            GameManager.instance.phase = GameManager.Phase.ready;
    }

    private void Update()
    {

        if (!_active)
            return;

        if (Input.GetMouseButtonDown(1) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
        /* régi verzió physic raycastos if (!_active) return;

         if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
         {
             Cancel();
             return;
         }

         if (!Input.GetMouseButtonDown(0)) return;
         print("thats");
         Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
         Collider2D hit = Physics2D.OverlapPoint(mousePos);   // pont-találat, nem sugár

         if (hit == null)
         {
             Cancel();          // üres helyre kattintás = megszakítás
             return;
         }

         ushort id = ResolveId(hit.transform);
         print("IT is id:" + id.ToString());
         if (id == ushort.MaxValue || !_valid.Contains(id))
             return;            // érvénytelen célpont: NEM szakítjuk meg, hadd próbálja újra

         var callback = _onPicked;
         Cancel();              // elõbb lezárjuk, csak utána hívunk

         callback?.Invoke(id);*/
    }
    public void TryPick(ushort id)
    {
        print("trying pick" +id.ToString());
        if (!_active)
            return;
        
        if (_valid == null || !_valid.Contains(id))
            return;
        
        var callback = _onPicked;

        Cancel();
        print("invoke");
        callback?.Invoke(id);
    }

    /// <summary>Lény vagy hõs a találatból. ushort.MaxValue = egyik sem.</summary>
    private static ushort ResolveId(Transform t)
    {
        var minion = t.GetComponent<LiveMinion>();
        print("MIniooon" + minion == null);
        if (minion != null) return minion.sequenceId;

        // ha lesz kattintható hõs:
        // var hero = t.GetComponent<LiveHero>();
        // if (hero != null) return hero.heroId;

        return ushort.MaxValue;
    }

    private void HighlightAll(bool on)
    {
        if (_valid == null) return;

        for (int i = 0; i < _valid.Count; i++)
        {
            var view = BoardManager.instance.GetMinion(_valid[i]);
            if (view != null) view.SetTargetHighlight(on);
        }
    }
}