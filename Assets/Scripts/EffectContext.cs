using System.Collections.Generic;

public class EffectContext
{
    public Effect effect;
    public ushort doerId;
    public MinionLogic Doer => new MinionLogic(doerId);
    public List<MinionLogic> targets;

    public PlayerController source;

    public PlayerController Enemy => GameManager.instance.OtherPlayer(source);

    public EffectContext(
        Effect e,
        ushort doerId,
        List<ushort> targetIds,
        PlayerController source)
    {
        this.effect = e;
        this.doerId = doerId;
        this.targets = new List<MinionLogic>();

        foreach (var targetId in targetIds)
        {
            var logic = GameManager.instance.GetMinionLogic(targetId);
            if (logic != null)
                this.targets.Add(logic);
        }

        this.source = source;
    }
}
