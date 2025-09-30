using System.Collections.Generic;
using UnityEngine;

public class EffectTester : MonoBehaviour
{
    void Start()
    {
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Test();
        }
    }
    void Test()
    {
        if(!GameManager.instance.IsClient)
        Debug.Log("=== EffectTester started ===");

        // GameManager dummy példány
        //var gmObj = new GameObject("GameManager");
        //GameManager.instance = gmObj.AddComponent<GameManager>();

        // Példa: hozz létre két miniont
        GameManager.instance.CreateMinionLogic(10); // minion ID = 10
        GameManager.instance.CreateMinionLogic(11); // minion ID = 11
        MinionState tmp = new MinionState
        {
            sequenceId = 11,
            cardId = 101,
            currentHealth = 8,
            attack = 3,
            canAttack = true,
            activeEffects = new List<ushort>()
        };
        
        if (GameManager.instance != null)
        GameManager.instance.AddAlly(tmp);

        var minion10 = GameManager.instance.GetMinionLogic(10);
        var minion11 = GameManager.instance.GetMinionLogic(11);

        // Adj nekik egy kezdő állapotot a MinionState-ben
        GameManager.instance.ChangeMinionById(10, minion=>minion=new MinionState
        {
            sequenceId = 10,
            cardId = 100,
            currentHealth = 5,
            attack = 2,
            canAttack = true,
            activeEffects = new List<ushort>()
        });

        GameManager.instance.ChangeMinionById(11, minion=>minion=new MinionState
        {
            sequenceId = 11,
            cardId = 101,
            currentHealth = 8,
            attack = 3,
            canAttack = true,
            activeEffects = new List<ushort>()
        });

        Debug.Log($"[Before] Minion 11 HP: {GameManager.instance.GetMinionById(11).currentHealth}");

        // Példa effekt: 3 damage minion10 → minion11
        Effect dmgEffect = new Effect
        {
            type = Effect.Type.damage,
            value = 3,
            target = Trigger.Target.enemy
        };

        var targets = new List<ushort> { minion11._sequenceId };
        var ctx = new EffectContext(dmgEffect, minion10._sequenceId, targets, null);

        EffectRunner.Run(ctx);

        Debug.Log($"[After] Minion 11 HP: {GameManager.instance.GetMinionById(11).currentHealth}");
    }
}

