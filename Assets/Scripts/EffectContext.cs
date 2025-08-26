using System.Linq;

using System.Collections.Generic;
public class EffectContext
{
    public Effect effect;
    public ILiveTarget doer;
    public List<ILiveTarget> targets;
    public PlayerController source;
    public PlayerController enemy;

    public EffectContext(Effect e, ILiveTarget doer, List<ILiveTarget> targets, PlayerController source, PlayerController enemy)
    {
        this.effect = e;
        this.doer = doer;
        this.targets = targets;
        this.source = source;
        this.enemy = enemy;
    }
}
