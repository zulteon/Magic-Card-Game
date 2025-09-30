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

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Attack(LiveMinion attacker,LiveMinion victim)
    {
        StartCoroutine(AttackIt(attacker,victim));
    }
    public IEnumerator AttackIt(LiveMinion attacker, LiveMinion victim, bool isHome = true)
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

    }
}
