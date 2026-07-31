using System;
using System.Collections.Generic;
using UnityEngine;

public static class EffectCommands
{
    public static Dictionary<Effect.Type, Action<EffectContext>> Registry = new()
    {
        { Effect.Type.damage, Damage },
        { Effect.Type.heal, Heal },
        { Effect.Type.give, Give },
        { Effect.Type.buff, Buff },
        {Effect.Type.attack,Attack }
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
            t.Damage(ctx.value,ctx.doerId);
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

    // További parancsok ide jöhetnek
}
