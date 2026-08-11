using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EffectCommands
{
    public static Dictionary<Effect.Type, Action<EffectContext>> Registry = new()
    {
        { Effect.Type.damage, Damage },
        { Effect.Type.heal, Heal },
        { Effect.Type.give, Give },
        { Effect.Type.buff, Buff },
        {Effect.Type.attack,Attack },
        {Effect.Type.charge,Charge },
        {Effect.Type.sleep,Sleep },
        {Effect.Type.copyStats,CopyStats },
        {Effect.Type.summon,Summon },
        {Effect.Type.doubleStats,DoubleStats},
        {Effect.Type.gainEconomy,GainEconomy},
       
        // add more as needed
    };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Initialize()
    {
        Debug.Log($"EffectCommands initialized. Commands: {Registry.Count}");
    }
    public static void Damage(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            t.Damage(ctx.value, ctx.doerId);
        }
    }
    public static void Charge(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            t.Charge();
        }
    }
    public static void CopyStats(EffectContext ctx)
    {
        if (ctx.targets.Length > 1) {Debug.LogWarning("COpy multiple stats? ");
        return;}
        GameManager.instance.GetMinionLogic(ctx.doerId).CopyStats(ctx.targetIds[0], ctx.effect.buff);
        
    }
    public static void DoubleStats(EffectContext ctx)
    {
        foreach(var t in ctx.targets)
        {
            t.DoubleStats(ctx.doerId, ctx.effect.buff);
        }
    }
    public static void Attack(EffectContext ctx)
    {
        Debug.Log("ctx target size " + ctx.targets.Length);
        GameManager.instance.ExecuteAttack(ctx.doerId,(ushort) ctx.targets[0]._sequenceId);
    }

    public static void Heal(EffectContext ctx)
    {
        if (ctx == null) { Debug.LogError("Nincs Context!"); return; }
        if (ctx.effect == null) { Debug.LogError("Nincs Effect adat a Contextben!"); return; }
        if (ctx.targets == null) { Debug.LogError("Nincs Target lista a Contextben!"); return; }

        foreach (var t in ctx.targets)
        {
            t.Heal(ctx.value, ctx.doerId); // ✨ Átadjuk a doerId-t!
        }
    }
    public static void Buff(EffectContext ctx)
    {
        if (ctx?.effect == null) { Debug.LogError("Buff: nincs Effect."); return; }
        if (ctx.targetIds == null) { Debug.LogError("Buff: nincs target lista."); return; }

        int attackBonus = ctx.effect.buff.x;
        int healthBonus = ctx.effect.buff.y;

        foreach (ushort id in ctx.targetIds)
            GameManager.instance.AddStats(id, attackBonus, healthBonus);
    }
    public static void Summon(EffectContext ctx)
    {
        bool toHome = true;
        if (ctx.effect.target == Trigger.Target.enemy || ctx.effect.target == Trigger.Target.ally)
            toHome = ctx.effect.target == Trigger.Target.ally;
        else
            Debug.LogWarning("A Summon effect nincs beállítva hogy home vagy enemy summon");

        int count = Mathf.Max(1, ctx.value);   // hány darabot idézzen

        for (int i = 0; i < count; i++)
            ctx.playerController.Summon(ctx.effect.summonableId, toHome);
    }
    public static void Sleep(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
            t.effectBag.Add(ctx.effect, t._sequenceId, EffectRole.Trigger,
                            charges: ctx.value);
    }
    public static void Give(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            if (t is ILiveTarget lt)
            {
                // például buffolás, hatás adás stb.
                lt.Attack += ctx.effect.buff.x;
                lt.Health += ctx.effect.buff.y;
            }
        }
    }
    public static void GainEconomy(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
            t.GainEconomy(ctx.doerId,ctx.value);
    }
    // További parancsok ide jöhetnek
}
