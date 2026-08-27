using System.Collections;
using UnityEngine;
using static Trigger;

public class CombatHandler : MonoBehaviour
{
    public static CombatHandler instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
    }
    public IEnumerator Attack(ushort attackerId, ushort victimId,int attackerNewHp,int victimNewHP)
    {
        LiveMinion attacker = BoardManager.instance.GetLiveMinion(attackerId);
        LiveMinion victim = BoardManager.instance.GetLiveMinion(victimId);
        
        yield return AttackIt(attacker, victim,attackerNewHp,victimNewHP);    
    }
    public void Attack(LiveMinion attacker,LiveMinion victim)
    {
        
        GameManager.instance.ExecuteAttack(attacker.sequenceId, victim.sequenceId);
    }//StartCoroutine(AttackIt(attacker,victim));
    public IEnumerator AttackIt(LiveMinion attacker, LiveMinion victim, int attackerNewHp, int victimNewHP, bool isHome = true)
    {
        if (attacker == null || victim == null) yield break;

        Transform a = attacker.transform;
        print("ATACK#####!" + a.name + ":" + victim.name);

        // A pozíciókat EGYSZER kérjük le. Ha a célpont közben meghal és
        // megsemmisül, az animáció akkor is végigfut a helyes koordinátákig.
        Vector3 startpos = a.position;
        Vector3 targetPos = victim.transform.position;
        Vector3 firstStation = startpos + new Vector3(0f, isHome ? 0.078f : -0.078f, 0f);

        // 1. Kis hátralépés
        float t = 0.2f, allT = t;
        while (t > 0)
        {
            if (a == null) yield break;
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(startpos, firstStation, (allT - t) / allT);
            yield return null;
        }

        // 2. Nekirepülés
        t = 0.5f; allT = t;
        while (t > 0)
        {
            if (a == null) yield break;
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(firstStation, targetPos, (allT - t) / allT);
            yield return null;
        }

        // SoundManager.inst.Hit();

        // 3. Becsapódás — itt jelenik meg a sebzés mindkét oldalon
        //if (attacker != null) attacker.AttackDamageApply(attacker.currentHealth - attackerNewHp);
        //if (victim != null) victim.AttackDamageApply(victim.currentHealth - victimNewHP);

        // 4. Vissza
        t = 0.7f; allT = t;
        while (t > 0)
        {
            if (a == null) yield break;
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(targetPos, firstStation, (allT - t) / allT);
            yield return null;
        }

        t = 0.2f; allT = t;
        while (t > 0)
        {
            if (a == null) yield break;
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(firstStation, startpos, (allT - t) / allT);
            yield return null;
        }

        if (a != null) a.position = startpos;
    }
}
