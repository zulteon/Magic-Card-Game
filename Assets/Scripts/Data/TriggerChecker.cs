using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class TriggerChecker : MonoBehaviour
{


    GameManager manager;
    public static TriggerChecker instance;

    private void Awake()
    {
        instance = this;
    }
    public void Start()
    {
        manager = GameManager.instance;
    }

    public static readonly Dictionary<Effect.LogicOperator, Func<bool, bool, bool>> Operators =
    new Dictionary<Effect.LogicOperator, Func<bool, bool, bool>>
    {
            { Effect.LogicOperator.AND, (a, b) => a && b },
            { Effect.LogicOperator.OR,  (a, b) => a || b },
            { Effect.LogicOperator.THEN, (a, b) => !a || b } // implikáció: a → b
    };
    bool FullOperationTrigger(List<Trigger> triggers, List<Effect.LogicOperator> operators, MinionLogic doer, MinionLogic targetLogic, EffectContext effect, int eventValue)
    {
        if (triggers.Count < 2) return IfSoTrigger(triggers[0], doer, targetLogic, effect, eventValue); // 1 trigger esetén

        bool result = Operators[operators[0]](
            IfSoTrigger(triggers[0], doer, targetLogic, effect, eventValue),
            IfSoTrigger(triggers[1], doer, targetLogic, effect, eventValue)
        );

        if (triggers.Count == 2) return result;

        // Chain további operátorokkal
        for (int i = 2; i < triggers.Count; i++)
        {
            result = Operators[operators[i - 1]](
                result,
                IfSoTrigger(triggers[i], doer, targetLogic, effect, eventValue)
            );
        }
        return result;
    }

    public bool IfSoTrigger(Trigger trigger, MinionLogic doerLogic,MinionLogic targetLogic,EffectContext effect=null, int eventValue=-1) 
    {
        if (trigger.sub == Trigger.subject.turn)
        {
            bool expectsOwnerTurn = (trigger.cond == Trigger.conditions.ally);

            bool isCardMine = !manager.IsEnemy(doerLogic);//or target?
            bool isMyTurn = GameManager.instance.isAllyTurn();
            bool isActuallyOwnerTurn = (isCardMine == isMyTurn);

            // 5. Ha a kártya gazda-kört vár, és az van, AKKOR True.
            // Ha a kártya ellenség-kört vár (false), és nem a gazda köre van (false), AKKOR is True.
            return expectsOwnerTurn == isActuallyOwnerTurn;
        }
        MinionState target=GameManager.instance.GetMinionById(targetLogic._sequenceId);
        MinionState doer=GameManager.instance.GetMinionById(doerLogic._sequenceId);
        int subjectValue=getSubject(trigger,targetLogic, target, eventValue);
        int value = trigger.value;
        if (trigger.valueTrigger!=null)
        {
            value =getSubject(trigger.valueTrigger,targetLogic,target, eventValue); 
        }


        switch (trigger.cond)
        {
            case Trigger.conditions.less:
                return subjectValue < value;
            case Trigger.conditions.equals:
                return subjectValue == value;
            case Trigger.conditions.more:
                return subjectValue > value;
        }return true;
    }

    int getSubject(Trigger trigger,MinionLogic targetLogic,MinionState target,int eventValue) {
        int value = 0;
        switch (trigger.sub)
        {
            case Trigger.subject.Attack:
                value =target.attack;
                break;
            case Trigger.subject.Health:
                value = target.currentHealth;
                break;
            case Trigger.subject.eventvalue:
                value = eventValue;
                break;

        }return value;
    }
    // Ezt a belső metódust hívja az összes többi
    private List<Effect> FilterByTrigger(IEnumerable<Effect> effects, Trigger.time targetTime)
    {
        if (effects == null) return new List<Effect>();

        return effects.Where(e =>
            e != null &&
            e.triggers != null &&
            e.triggers.Any(trig => trig.t == targetTime)
        ).ToList();
    }
    
    public List<Effect> CheckTrigger(Trigger.time trigger, MinionData data)
        => FilterByTrigger(data.e, trigger);

    public List<Effect> CheckTrigger(Trigger.time trigger, ushort minionId)
        => FilterByTrigger(manager.GetMinionEffects(minionId), trigger);

    public List<Effect> CheckTrigger(Trigger.time trigger, MinionCard minion) 
        => FilterByTrigger(EffectManagerClient.instance.GetEffectData(minion.effectIds), trigger);
    public List<Effect> CheckTrigger(Trigger.time trigger, Effect.Type activity, ushort minionId)
    => FilterByTrigger(manager.GetMinionEffects(minionId), trigger, activity);
    public List<Effect> CheckTrigger(List<Effect> effects, Trigger.time targetTime)=> FilterByTrigger(effects, targetTime);
    private List<Effect> FilterByTrigger(IEnumerable<Effect> effects, Trigger.time targetTime, Effect.Type targetActivity)
    {
        if (effects == null) return new List<Effect>();
        return effects.Where(e =>
            e != null &&
            e.triggers != null &&
            e.triggers.Any(trig => trig.t == targetTime && trig.activity == targetActivity)
        ).ToList();
    }
    // TriggerChecker — új overload
    public List<Effect> CheckTrigger(Trigger.time trigger, CardData card)
    {
        var ids = GetEffectIds(card);
        Debug.Log($"[CheckTrigger] card={card.cardId} type={card.GetCardType()} ids={string.Join(",", ids)}");

        var effects = EffectManagerClient.instance.GetEffectData(ids);
        Debug.Log($"[CheckTrigger] betöltött effektek: {effects.Count}");
        foreach (var e in effects)
            Debug.Log($"  - {e.name} type={e.type} triggerCount={e.triggers?.Length ?? 0}");

        var filtered = FilterByTrigger(effects, trigger);
        Debug.Log($"[CheckTrigger] szűrt eredmény ({trigger}): {filtered.Count}");

        return filtered;
    }

    private static List<ushort> GetEffectIds(CardData card) => card switch
    {
        MinionCard m => m.effectIds,
        SpellCard s => s.effectIds,
        _ => new List<ushort>()
    };
    public bool IsBattlecry(Effect effect)
    {
        if (effect == null || effect.triggers == null )
            return false;

        return effect.triggers[0].name.ToLower()== "onplay";
    }
    public bool IsDeathRettle(Effect effect)
    {
        if (effect == null || effect.triggers == null)
            return false;
        print(" Effect " + effect.type.ToString() + effect.effectId.ToString());
        return effect.triggers[0].activity == Effect.Type.death;//name.ToLower() == "ondeath";
    }
    public List<Effect> GetBattlecries(MinionCard minion)
    {
        List<Effect> allEffects = EffectManagerClient.instance.GetEffectData(minion.effectIds);
        return allEffects.FindAll(e => IsBattlecry(e));
    }
    public List<Effect> GetOnDeathEffect(ushort minionId)
    {
        List<ushort>  effects=
            GameManager.instance.GetMinionById(minionId).activeEffects;
        print(" ACTIVE EFFECTS " + effects.Count.ToString());
         return EffectManagerClient.instance.GetEffectData(effects).FindAll(e=> IsDeathRettle(e));
    }
    public List<Effect> GetOnDeathEffect(MinionData minion)
    {
        return minion.e.FindAll(e => IsDeathRettle(e));
    }
    public bool IsDoerValid(Trigger trigger, MinionLogic doer, ushort ownerId)
    {
        switch (trigger.tar)
        {
            case Trigger.Target.none:
            case Trigger.Target.all:
                return true;

            case Trigger.Target.self:
                return doer._sequenceId == ownerId;

            case Trigger.Target.allother:
                return doer._sequenceId != ownerId;

            case Trigger.Target.ally:
                return manager.IsEnemy(doer) == manager.IsEnemy(GameManager.instance.GetMinionLogic(ownerId));

            case Trigger.Target.enemy:
                return manager.IsEnemy(doer) != manager.IsEnemy(GameManager.instance.GetMinionLogic(ownerId));

            default:
                return true;   // adjacent / left / right — pozíció, később
        }
    }
}
