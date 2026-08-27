using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Trigger;

public  static class TargetingCenter
{
    public static GameManager GameManager { get; private set; }
    public static List<ushort> GetTargets(Effect e, ushort doerId, PlayerController source=null)
    {// az egész playercontroller lekérést kilehetne szedni , fölösleges elég annyit tudni hogy ally vagy nem 
        if (Trigger.Target.none==e.target) return null;
        if(e.targetCast==Effect.TargetCast.single&& e.target==Trigger.Target.self)
            return new List<ushort>() { doerId};
        if (source == null)
        {
            source = GameManager.instance.GetOwnerOf(doerId);
        }
        Debug.Log($"[GetTargets] source GetOwnerOf után: {(source == null ? "MÉG MINDIG NULL" : "ok")}");
        PlayerController enemy = GameManager.instance.GetEnemy(source);
        List<ushort> targetIds = new List<ushort>();
        
        // például: forrás minionjai
        var sourceIds = GameManager.instance.GetAlly(!source.isEnemy.Value)
            .Select(m => m.sequenceId)
            .ToList();

        var enemyIds = GameManager.instance.GetEnemyBoard(!source.isEnemy.Value)
            .Select(m => m.sequenceId)
            .ToList();

        switch (e.target)
        {
            case Trigger.Target.self:
                targetIds.Add(doerId);
                break;
            case Trigger.Target.ally:
                targetIds.Add(source.heroId);
                targetIds.AddRange(sourceIds);
                break;
            case Trigger.Target.enemy:
                targetIds.Add(enemy.heroId);
                targetIds.AddRange(enemyIds);
                break;
            case Trigger.Target.all:
                targetIds.Add(source.heroId);
                targetIds.Add(enemy.heroId);
                targetIds.AddRange(sourceIds.Concat(enemyIds));
                break;
            case Trigger.Target.allother:
                targetIds.Add(source.heroId);
                targetIds.Add(enemy.heroId);
                targetIds.AddRange(sourceIds.Concat(enemyIds));
                targetIds.Remove(doerId);
                break;
            case Trigger.Target.adjacent:
                targetIds.AddRange(
                GameManager.instance.GetNeighbours(doerId, GameManager.instance.isAlly(source)));
                break;
            
        }
        if(e.other)targetIds.Remove(doerId);
        Debug.Log($"[GetTargets] targetType={e.targetType} targetIds count ELÕTTE={targetIds.Count}");
        return FilterAndSelectTargets(doerId,targetIds, e);
    }
    public static List<ushort> FilterAndSelectTargets(
       ushort doerId,
       List<ushort> targetIds,
       Effect e)
    {
        
        // 1. Típus szerinti szûrés
        // itt MinionState-et kell lekérni a GameManagerbõl
        //var selectedTarget = targetIds;

        switch (e.targetType)
        {
            case Trigger.TargetType.character:
                // minden marad
                break;

            case Trigger.TargetType.minion:
                targetIds = targetIds.Where(s =>!IsHero(s)).ToList(); 
                break;

            case Trigger.TargetType.hero:
                // itt a hero külön jön, nem MinionState
                targetIds = targetIds.Where(s => IsHero(s)).ToList();
                break;

            case Trigger.TargetType.race:
                targetIds = targetIds.Where(s =>
                {
                    var st = GameManager.instance.GetMinionById(s);
                    var mc = CardManager.instance.GetMinion(st.cardId);
                    return mc != null && mc.raceId == e.raceValue;
                }).ToList();
                break;

            default:
                Debug.LogWarning("Unknown target type: " + e.targetType);
                break;
        }
        Debug.Log($"[GetTargets] targetType={e.targetType} targetIds count ELÕTTE={targetIds.Count}");
        if (targetIds.Count == 0)
            return new List<ushort>();
        // típus-szûrés után, mielõtt a targetCast szerint válogatnál:
        if (e.sortMode != Trigger.SortMode.none)
        {
            targetIds = SortByMode(targetIds, e.sortMode);
        }
        // 2. Végsõ kiválasztás
        List<ushort> result = new List<ushort>();
        switch (e.targetCast)
        {
            case Effect.TargetCast.all:
                return targetIds;

            case Effect.TargetCast.single:
                if (targetIds.Count == 1)
                {
                    result.Add(targetIds[0]);
                }
                else if (e.random)
                {
                    var rnd = Random.Range(0, targetIds.Count);
                    result.Add(targetIds[rnd]);
                }
                else
                {
                    return targetIds;
                    //Debug.Log("TODO: Player manual target selection");
                   // return null;
                }
                break;

            case Effect.TargetCast.multi:
                int multivalue = Mathf.Min(targetIds.Count, e.multiValue);

                if (e.random)
                {
                    if (e.multiSplit)
                    {
                        for (int i = 0; i < multivalue; i++)
                        {
                            var rnd = Random.Range(0, targetIds.Count);
                            result.Add(targetIds[rnd]);
                        }
                    }
                    else
                    {
                        var shuffled = targetIds.OrderBy(_ => Random.value).ToList();
                        result = shuffled.Take(multivalue).Select(c => c).ToList();
                    }
                }
                else
                {
                    Debug.Log("TODO: Player selects multiple targets");
                    return result;
                }
                break;
        }
        Debug.Log($"[GetTargets] targetType={e.targetType} targetIds count ELÕTTE={targetIds.Count}");
        return result;
    }
    private static bool IsHero(ushort id)
    {
        return id == 0 || id == 1;
    }

 /*   public static List<ILiveTarget> FilterByType(ILiveTarget doer,List<ILiveTarget> targets,Effect e)
    {
        switch (e.targetType)
        {
            case Trigger.TargetType.character:
                
                return targets;
            case Trigger.TargetType.minion:
                targets = targets.Where(t => t.type == ILiveTarget.Type.minion).ToList();
                break;
            case Trigger.TargetType.hero:
                targets = targets.Where(t => t.type == ILiveTarget.Type.hero).ToList();
                break;
            case Trigger.TargetType.race:
                targets = targets.Where(t => (int)t.race == e.raceValue).ToList();
                break;
            default:
                Debug.LogWarning("Unknown target type: " + e.targetType);
                break;
        }
        return targets;
    }
   
    public static List<ILiveTarget> SelectFinalTargets(List<ILiveTarget> targets, Effect e) {
        if (targets.Count == 0)
            return new List<ILiveTarget>();
        List<ILiveTarget> result= new List<ILiveTarget>();
        switch (e.targetCast){

            case Effect.TargetCast.all:
                return targets;
            case Effect.TargetCast.single:
                if (targets.Count == 1)
                {
                    result.Add(targets[0]);
                    return result;
                }

                if (e.random)
                    result.Add(targets[Random.Range(0, targets.Count)]);
                else

                    //call the selection 
                    //TODO!!
                    return null;
                    Debug.Log("call selection here");
                break;
            case Effect.TargetCast.multi:
                if (e.random)
                {
                    int multivalue = Mathf.Min(targets.Count, e.multiValue);
                    
                    // multi split : 
                    if (e.multiSplit)
                    {

                        for (int i = 0; i < multivalue; i++)
                        {
                            result.Add(targets[Random.Range(0, targets.Count)]);
                        }
                    }
                    else
                    {
                        var shuffled = targets.OrderBy(_ => Random.value).ToList();
                        result = shuffled.Take(multivalue).ToList();
                    }
                     
                }
                else
                {
                    // probably dont need that for a while
                }
                break;
                    
            default:
                Debug.LogWarning("Unknown target cast: " + e.targetCast);
                break;
        }
        return result;
    }*/
    private static List<ushort> SortByMode(List<ushort> ids, Trigger.SortMode mode)
    {
         switch (mode)
         {
             case Trigger.SortMode.highestHealth:
                 return ids.OrderByDescending(id => GameManager.instance.GetMinionById(id).currentHealth).ToList();
             case Trigger.SortMode.lowestHealth:
                 return ids.OrderBy(id => GameManager.instance.GetMinionById(id).currentHealth).ToList();
            case Trigger.SortMode.highestAttack:
                 return ids.OrderByDescending(id => GameManager.instance.GetMinionById(id).attack).ToList();
             case Trigger.SortMode.lowestAttack:
                 return ids.OrderBy(id => GameManager.instance.GetMinionById(id).attack).ToList();
             default:
                 return ids;
         }
    }
    
}