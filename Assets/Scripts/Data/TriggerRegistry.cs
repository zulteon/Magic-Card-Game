using System.Collections.Generic;

public class TriggerRegistry
{
    // Dictionary: trigger t�pus -> lista azokr�l a minionokr�l, akiknek van ilyen trigger�k
    private Dictionary<Trigger, List<MinionLogic>> registeredTriggers = new();

    public void RegisterTrigger(Trigger triggerType, MinionLogic minion)
    {
        if (!registeredTriggers.ContainsKey(triggerType))
            registeredTriggers[triggerType] = new List<MinionLogic>();

        registeredTriggers[triggerType].Add(minion);
    }

    public void UnregisterTrigger(Trigger triggerType, MinionLogic minion)
    {
        if (registeredTriggers.ContainsKey(triggerType))
            registeredTriggers[triggerType].Remove(minion);
    }

    public List<MinionLogic> GetMinionsWithTrigger(Trigger triggerType)
    {
        return registeredTriggers.GetValueOrDefault(triggerType, new List<MinionLogic>());
    }
}