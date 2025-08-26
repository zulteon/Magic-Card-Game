using System.Collections.Generic;

public static class EffectRunner
{
    public static void Run(Effect effect, ILiveTarget doer, List<ILiveTarget> targets, PlayerController source, PlayerController enemy)
    {
        var ctx = new EffectContext(effect, doer, targets, source, enemy);

        if (EffectCommands.Registry.TryGetValue(effect.type, out var command))
        {
            command(ctx);
        }
        else
        {
            UnityEngine.Debug.LogWarning($"No command found for effect type: {effect.type}");
        }
    }
    public static void Run(EffectContext ctx)
    {
        

        if (EffectCommands.Registry.TryGetValue(ctx.effect.type, out var command))
        {
            command(ctx);
        }
        else
        {
            UnityEngine.Debug.LogWarning($"No command found for effect type: {ctx.effect.type}");
        }
    }
}
