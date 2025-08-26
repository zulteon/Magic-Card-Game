using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public  static class TargetingCenter
{
    public static GameManager GameManager { get; private set; }
    public static List<ILiveTarget> GetTargets(Effect e, ILiveTarget doer, PlayerController source, PlayerController enemy)
    {
        // A c�lpontok list�j�t nem kell inicializ�lni, k�s�bb �p�tj�k fel
        List<ILiveTarget> targets = new List<ILiveTarget>();

        // Mindenekel�tt a board list�kat �t kell alak�tanunk ILiveTarget list�kk�
        // A .Select(netObj => netObj.GetComponent<ILiveTarget>()) a kulcs!
        var sourceBoardTargets = GameManager.GetAlly(!source.isEnemy)
            .Where(target => target != null) // Kisz�rj�k, ha valami�rt nem ILiveTarget
            .ToList();

        var enemyBoardTargets = GameManager.GetEnemyBoard(!source.isEnemy)
            .Where(target => target != null)
            .ToList();

        switch (e.target)
        {
            case Trigger.Target.self:
                targets = new List<ILiveTarget>();
                // Ez a r�sz j�, mert a "doer" m�r ILiveTarget
                if (doer.type != ILiveTarget.Type.hero)
                    targets.Add(doer);
                break;

            case Trigger.Target.ally:
                // Hozz�adjuk a h�s�nket �s a l�nyeinket
                targets.Add(source.hero);
                targets.AddRange(sourceBoardTargets);
                break;

            case Trigger.Target.enemy:
                // Hozz�adjuk az ellenf�l h�s�t �s l�nyeit
                targets.Add(enemy.hero);
                targets.AddRange(enemyBoardTargets);
                break;

            case Trigger.Target.all:
                // A h�s�ket �s az asztalokat �sszef�zz�k
                targets.Add(source.hero);
                targets.Add(enemy.hero);
                targets.AddRange(sourceBoardTargets.Concat(enemyBoardTargets));
                break;

            case Trigger.Target.allother:
                // Az �sszes c�lpontot �sszegy�jtj�k, majd elt�vol�tjuk a doer-t
                targets.Add(source.hero);
                targets.Add(enemy.hero);
                targets.AddRange(sourceBoardTargets.Concat(enemyBoardTargets));
                targets.Remove(doer);
                break;

            default:
                Debug.LogWarning("Unknown target : " + e.target);
                return new List<ILiveTarget>();
        }

        if (e.other)
            targets.Remove(doer);

        // Ezt a r�szt a megl�v� logik�d alapj�n kell meg�rni,
        // felt�telezve, hogy a FilterByType �s a SelectFinalTargets
        // met�dusok ILiveTarget list�kat v�rnak.
        targets = FilterByType(doer, targets, e);
        targets = SelectFinalTargets(targets, e);

        return targets;
    }/*
    public static List<ILiveTarget> GetTargets(Effect e,ILiveTarget doer, PlayerController source, PlayerController enemy, string offline="ja")
    {
        List<ILiveTarget> targets=new List<ILiveTarget> ();
        targets.Add(source.hero);
        targets.Add(enemy.hero);
        switch (e.target)
        {
            case Trigger.Target.self:
                targets=new List<ILiveTarget>();
                if(doer.type!=ILiveTarget.Type.hero)
                    targets.Add(doer);
                break;
            case Trigger.Target.ally:
                targets.Remove(enemy.hero) ;

                targets.AddRange(source.board);
                
                break;
            case Trigger.Target.enemy:
                targets.Remove(source.hero);
                targets.AddRange(enemy.board);
                
                break;
            case Trigger.Target.all:
                targets.AddRange (source.board.Concat(enemy.board));
                break;
            case Trigger.Target.allother:
                targets.AddRange(source.board.Concat(enemy.board));
                targets.Remove(doer);
                break;
            
            
            default:
                Debug.LogWarning("Unknown target : " + e.target);
                return new List<ILiveTarget>();
        }
        if (e.other)
            targets.Remove(doer);
        targets = FilterByType(doer, targets, e);
        targets = SelectFinalTargets(targets, e);
        return targets;
        
    }*/
    
    public  static List<ILiveTarget> FilterByType(ILiveTarget doer,List<ILiveTarget> targets,Effect e)
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
    }
}