using System.Collections;
using UnityEngine;

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
        Transform a = attacker.transform;
        Transform b = victim.transform;
        print("ATACK#####!" + a.name + ":" + b.name);

        Vector3 startpos = a.transform.position;
        float t = 0.2f;
        float allT = t;
        Vector3 firstStation = a.position + new Vector3(0f, isHome ? 0.078f : -0.078f, 0f);
        while (t > 0)
        {
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(a.position, firstStation, (allT - t) / allT);
            yield return null;
        }
        float dist = Vector3.Distance(a.transform.position, b.transform.position);
        Vector3 tmp = a.position;
        t = 0.5f; allT = t;
        while (t > 0)
        {
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(tmp, b.position, (allT - t) / allT);
            yield return null;
        }
        // SoundManager.inst.Hit();
        //damage
        //GameManager.instance.Attack();
        attacker.AttackDamageApply(
           attacker.currentHealth - attackerNewHp);
        Vector3 endPos = a.position;
        t = 0.7f; allT = t;
        while (t > 0)
        {
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(b.position, firstStation, (allT - t) / allT);
            yield return null;
        }
        t = 0.2f; allT = t;
        while (t > 0)
        {
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(firstStation, startpos, (allT - t) / allT);
            yield return null;
        }
        a.position = startpos;
        // yield return hogy az effect rendszer bevárja
        attacker.AttackDamageApply(
           attacker.currentHealth - attackerNewHp);
        victim.AttackDamageApply(
           victim.currentHealth - victimNewHP);
    }
}
