using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Effect", menuName = "Card Creator/effect", order = 1)]
public class Effect : ScriptableObject
{

    public enum Type
    {
        none, damage, heal, give, death, attack, buff, steal, swapAttackHealth, setStats, copyStats, gainEconomy, counter, spell, cardDestroyed,
        charge, summon,sleep,taunt,doubleStats,cleave
    }
    /*public enum Type{none,all,cleave,windfury,buff,have,ranged,
        damaged,arrange,execute,steal,roll,death,target,rolled,
        highlight,extraspace,level,bomb,summon,gold,freeroll,spy,boardHave,instantAttack,attack};

   /* public enum Type {attack, add,summon, damage,execute, play,bubble, buff, death, 
        transform, immune,change,copy,multiply,none,taunt, give,windfury,shoot ,destroy,eat,cleave,
        gainGold,merge,silence,gainArmor,cheapLevelnd,ressurect,buy,cheapMinion,have,
        lessdamage,flying,addmysticcard,mysticvalue,eatandgold,damaged,eatandStats,
        sell,refresh,levelUp,turnEnded,Defend,bomb,freerefresh
    }*/

    public enum TargetCast : byte { single, multi, random, all, each, left }
    public enum TargetType : byte { minion, race, hero, character, shop, board }
    public Type type;
    public Trigger.TargetType targetType;
    public TargetCast targetCast;
    public Trigger.Target target;
    public int value;
    public Vector2Int buff = new Vector2Int(0, 0);
    public ushort summonableId;
    public Effect give;
    public Trigger[] triggers;
    public bool random = false;
    public int raceValue = -1;
    public int multiValue = -1;
    public bool multiSplit = false;
    public bool other = false;
    public ushort effectId;
    [Header("Logic Operators")]
    public List<LogicOperator> logicOp = new List<LogicOperator>() { };
    public Trigger linkedTrigger; // következő trigger a láncban
    public Trigger.SortMode sortMode;
    public enum LogicOperator { NONE, AND, OR, THEN }
    public Zone activeZone = Zone.Board;
#if UNITY_EDITOR
private void OnValidate()
{
    if(name=="Effect")return;
    if (effectId != 0) return;

    UnityEditor.EditorApplication.delayCall += () =>
    {
        if (this == null || effectId != 0) return;

        var guids = UnityEditor.AssetDatabase.FindAssets("t:Effect",
            new[] { "Assets/Real_Cards/Abilities" });

        ushort max = 0;
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var e = UnityEditor.AssetDatabase.LoadAssetAtPath<Effect>(path);
            if (e != null && e != this && e.effectId > max) max = e.effectId;
        }

        effectId = (ushort)(max + 1);
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    };
}
#endif
}

