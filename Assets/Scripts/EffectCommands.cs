using System;
using System.Collections.Generic;
using UnityEngine;

public static class EffectCommands
{
    public static Dictionary<Effect.Type, Action<EffectContext>> Registry = new()
    {
        { Effect.Type.damage, Damage },
        { Effect.Type.heal, Heal },
        { Effect.Type.give, Give }
        // add more as needed
    };

    public static void Damage(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            t.Damage(ctx.effect.value);
        }
    }

    public static void Heal(EffectContext ctx)
    {
        foreach (var t in ctx.targets)
        {
            t.Heal(ctx.effect.value);
        }
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
