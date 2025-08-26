using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Effect", menuName = "Card Creator/effect", order = 1)]
public class Effect : ScriptableObject
{
    
    public enum Type:byte { none,damage,heal,give}
    /*public enum Type{none,all,cleave,windfury,buff,have,ranged,
        damaged,arrange,execute,steal,roll,death,target,rolled,
        highlight,extraspace,level,bomb,summon,gold,freeroll,spy,boardHave,instantAttack,attack};

   /* public enum Type {attack, add,summon, damage,execute, play,bubble, buff, death, 
        transform, immune,change,copy,multiply,none,taunt, give,windfury,shoot ,destroy,eat,cleave,
        gainGold,merge,silence,gainArmor,cheapLevelnd,ressurect,buy,cheapMinion,have,
        lessdamage,flying,addmysticcard,mysticvalue,eatandgold,damaged,eatandStats,
        sell,refresh,levelUp,turnEnded,Defend,bomb,freerefresh
    }*/
    
    public enum TargetCast :byte { single, multi, random, all, each,left }
    public enum TargetType :byte { minion, race,hero, character, shop, board }
    public Type type;
    public Trigger.TargetType targetType;
    public TargetCast targetCast;
    public Trigger.Target target;
    public int value;
    public Vector2Int buff = new Vector2Int(0,0);
    public MinionData summonable;   
    public Effect give;
    public Trigger[] triggers;
    public bool random=false;
    public int raceValue = -1;
    public int multiValue = -1;
    public bool multiSplit = false;
    public bool other = false;
    public ushort effectId;
}


/*
 * Dictionary<Effect.Type, Func<IEffectCommand>> registry = new Dictionary<Effect.Type, Func<IEffectCommand>>()
{
    { Effect.Type.damage, () => new DamageCommand() },
    { Effect.Type.heal, () => new HealCommand() },
    { Effect.Type.give, () => new GiveCommand() }
};
IEffectCommand command;
if (registry.TryGetValue(effect.type, out var factory))
{
    command = factory();
    command.Execute();
}
 
 */