using System.Collections.Generic;
using UnityEngine;
using System;

public class TriggerChecker : MonoBehaviour
{




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

    bool IfSoTrigger(Trigger trigger, MinionLogic doerLogic,MinionLogic targetLogic,EffectContext effect, int eventValue) 
    {
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
    
}
