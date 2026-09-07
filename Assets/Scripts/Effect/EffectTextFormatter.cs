using System.Collections.Generic;
using System.Text;

public static class EffectTextFormatter
{
    public static string[] Build(MinionLogic minion)
    {
        if (minion == null ||
            minion.effectBag == null ||
            minion.effectBag.Count == 0)
        {
            return new string[0];
        }

        List<string> result = new List<string>();

        foreach (LiveEffect effect in minion.effectBag.All)
        {
            if (effect == null)
                continue;

            string text = FormatEffect(effect);

            if (!string.IsNullOrWhiteSpace(text))
                result.Add(text);
        }

        return result.ToArray();
    }

    private static string FormatEffect(LiveEffect effect)
    {
        StringBuilder sb = new StringBuilder();

        // ─────────────────────────────
        // EFFECT TYPE
        // Ez a legfontosabb információ.
        // ─────────────────────────────

        sb.AppendLine(GetEffectType(effect));

        // ─────────────────────────────
        // EXTRA LIVE INFORMATION
        // A Role-t NEM írjuk ki,
        // csak eldöntjük vele, mi releváns.
        // ─────────────────────────────

        switch (effect.Role)
        {
            case EffectRole.Guard:
                {
                    if (effect.toBlock != null)
                    {
                        sb.AppendLine(
                            $"Blocks: {effect.toBlock}"
                        );
                    }

                    AppendCharges(sb, effect);
                    AppendFrequency(sb, effect);

                    break;
                }

            case EffectRole.Trigger:
                {
                    AppendCharges(sb, effect);
                    AppendFrequency(sb, effect);

                    break;
                }

            case EffectRole.Delayed:
                {
                    AppendCharges(sb, effect);

                    if (effect.remainingTurns >= 0)
                    {
                        string turnText =
                            effect.remainingTurns == 1
                                ? "turn"
                                : "turns";

                        sb.AppendLine(
                            $"Expires in: {effect.remainingTurns} {turnText}"
                        );
                    }

                    break;
                }

            case EffectRole.Aura:
                {
                    // Aura esetén jelenleg csak
                    // az effect type jelenik meg.
                    break;
                }
        }

        return sb.ToString().TrimEnd();
    }

    private static string GetEffectType(LiveEffect effect)
    {
        if (effect.Def == null)
            return $"Unknown Effect ({effect.effectId})";

        return effect.Def.type.ToString();
    }

    private static void AppendCharges(
        StringBuilder sb,
        LiveEffect effect)
    {
        if (effect.charges < 0)
            return;

        sb.AppendLine(
            $"Remaining charges: {effect.charges}"
        );
    }

    private static void AppendFrequency(
        StringBuilder sb,
        LiveEffect effect)
    {
        if (effect.howOften <= 1)
            return;

        sb.AppendLine(
            $"Triggers every {effect.howOften} times"
        );
    }
}