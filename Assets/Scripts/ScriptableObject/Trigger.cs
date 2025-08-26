using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "trigger", menuName = "Card Creator/trigger", order = 2)]
public class Trigger : ScriptableObject
{
    // Start is called before the first frame update
    //public enum Trigger { instant, endofturn, startofturn, dormant, }
        // instant is like battlecry
    public enum time { instant,during, endofturn, startofturn, dormant,when, after,oneturn,multiturn,startofcombat,endofcombat,ifso,value }
    public enum subject{ Minion,Card,None,Copy,Hero,Health,Attack, Attack_Health,Self,LastBattleDamage,minionCount }
    public enum conditions { less,adjacent,more,none,equals,thisOne}
    public enum Target {self, all,enemy,ally, adjacent,allother,left,right,none}
    public enum TargetType { minion,race,character,taunt,hero,none}
    public enum TargetCast { single, multi, random, all,none }
    public int racevalue = -1;
    public int multiValue = -1;
    public int value=-1;
    public Vector2 stats=new Vector2(-1,-1);
    public time t;
    public subject sub=subject.None;
    public conditions cond=conditions.none;
    public Effect.Type activity = Effect.Type.none;
    public Target tar=Trigger.Target.none;
    public TargetType tartype=TargetType.none;
    public TargetCast targetcast=TargetCast.none;
}


