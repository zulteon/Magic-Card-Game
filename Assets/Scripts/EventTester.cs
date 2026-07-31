using UnityEngine;
using System.Collections.Generic;
public class EventTester : MonoBehaviour
{
    [Header("Settings")]
    public ushort sequenceId=3; // Az egyedi azonosítód (pl. 55)
    public int health = 3;

    [Header("Current State")]
    public bool hasShield = false;
    EffectContext effectContext;
    void Start()
    {
        // --- 1. RÉTEG: Feliratkozás több eseményre ---

        // Kör elején akarunk pajzsot kapni
        
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            TickIn();
        }
    }

    // --- 2. RÉTEG: Logika a kör elején (Morning) ---
    private void OnMorning()
    {
        hasShield = true;
        Debug.Log($"<color=green>[START EVENT {sequenceId}]</color> Jó reggelt! Pajzs aktiválva.");
    }

    // --- 3. RÉTEG: Logika a kör végén (Evening) ---
    private void OnEvening()
    {
        if (hasShield)
        {
            hasShield = false;
            Debug.Log($"<color=yellow>[END EVENT {sequenceId}]</color> A pajzs megvédett a kör végi sebzéstõl, de elhasználtam!");
        }
        else
        {
            health--;
            Debug.Log($"<color=red>[END EVENT {sequenceId}]</color> Sebzõdtem! Maradék HP: {health}");

            if (health <= 0)
            {
                HandleDeath();
            }
        }
    }

    private void HandleDeath()
    {
        Debug.Log($"<color=black>[DEATH {sequenceId}]</color> Meghaltam. Események törlése...");

        // --- 4. RÉTEG: Automatikus takarítás ---
        // Ez garantálja, hogy a következõ körben már ne fusson le semmilyen kódja
        GameEvents.Instance.ClearMinion(sequenceId);

        Destroy(gameObject);
    }
    public void TickIn()
    {
        GameEvents.Instance.AddEvent(sequenceId, GameEvents.EventType.TurnStart, OnMorning);
        GameEvents.Instance.AddEvent(sequenceId, GameEvents.EventType.TurnEnd, OnEvening);
        //<<<<<<<<<<<<<############################>>>>>>>>>>>>>
        Effect dmgEffect = new Effect
        {
            type = Effect.Type.attack,
            value = 2,
            target = Trigger.Target.enemy,
        };

        var targets = new List<ushort> { 2 };
        effectContext = new EffectContext(dmgEffect, 3,targets);
        GameEvents.Instance.AddEvent(sequenceId, GameEvents.EventType.TurnEnd, () => EffectRunner.Run(effectContext));

        Debug.Log($"<color=cyan>[EventTester {sequenceId}]</color> Bekapcsolva. HP: {health}, Stratégia: Reggel pajzs, este sebzés.");
        GameEvents.Instance.RaiseTurnStart();
        GameEvents.Instance.RaiseTurnEnd();
    }
}