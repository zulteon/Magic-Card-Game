using System.Collections.Generic;

public class TriggerRegistry
{
    // Dictionary: trigger típus -> lista azokról a minionokról, akiknek van ilyen triggerük
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