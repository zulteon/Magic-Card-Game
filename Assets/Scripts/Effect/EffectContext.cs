using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;// majd vedd ki ezt a sort
public class EffectContext  // ← Vissza class-ra
{
    public Effect effect;
    public ushort doerId;
    public MinionLogic[] targets;// tudatosan minion logicokat adunk???
    public int value;
    public ushort[] targetIds;
    public PlayerController playerController;
    public Vector2Int buff;
    public EffectContext(Effect e, ushort doerId, List<ushort> targetIds=null, ushort extraValue = 0,PlayerController source=null)
    {
        if (source == null)
        {
            source = GameManager.instance.GetOwnerOf(doerId);
        }
        playerController = source; // az egyszerüség kedvéért rámentjük 

        if (targetIds == null)
        {// maybe the targeting center get targets can drop minion logic but thats a micro performance
            //after a while it turned out very usefull, when we add events with empty targets 
            targetIds=TargetingCenter.GetTargets(e, doerId,source);
            UnityEngine.Debug.Log("Are we hetting here"+ targetIds.Count);
        }
        this.effect = e;
        this.doerId = doerId;
        this.value = e.value + extraValue;
        this.buff = effect.buff;
        MinionLogic[] temp = new MinionLogic[targetIds.Count];
        int count = 0;

        for (int i = 0; i < targetIds.Count; i++)
        {
            var m = GameManager.instance.GetMinionLogic(targetIds[i]);
            if (m != null)
            {
                temp[count] = m;
                count++;
            }
            else UnityEngine.Debug.Log($"<color=red>whaat</color> {targetIds[i]}");
        }

        // csak a valóban talált logicokat adjuk át, nem a teljes (esetleg null-os) temp tömböt
        this.targets = new MinionLogic[count];
        System.Array.Copy(temp, this.targets, count);
        this.targetIds = targetIds.ToArray();


        UnityEngine.Debug.Log(" csekkoljuk  a value triggert");
        OverWriteValueTrigger();
    }
    public ClientEvent ToClientEvent()
    {
        ushort[] ids = Array.Empty<ushort>();
        int[] healthValues = Array.Empty<int>();

        if (targets != null && targets.Length > 0)
        {
            ids = new ushort[targets.Length];
            healthValues = new int[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                ids[i] = targets[i]._sequenceId;
                healthValues[i] = targets[i].Health;
            }
        }

        return new ClientEvent
        {
            effectType = (ushort)effect.type,
            targetIds = ids,
            value = value,
            doerId = doerId,
            newValues = healthValues
        };
    }/*

    public ClientEvent ToClientEvent()
    {
        ushort[] ids = new ushort[targets.Length];
        int[] healthValues = new int[targets.Length]; // ✨ ÚJ!

        for (int i = 0; i < targets.Length; i++)
        {
            ids[i] = targets[i]._sequenceId;
            healthValues[i] = targets[i].Health; // ✨ Aktuális HP snapshot
        }

        return new ClientEvent
        {
            effectType = (ushort)effect.type,
            targetIds = ids,
            value = value,
            doerId = doerId,
            newValues = healthValues // ✨ Pillanatkép!
        };
    }*/
    void OverWriteValueTrigger()
    {
        if (this.effect.triggers.Length <= 1) return;

        bool ally = GameManager.instance.isAllyMinion(this.doerId);

        for (int i = 1; i < this.effect.triggers.Length; i++)
        {
            Trigger t = this.effect.triggers[i];

            if (t.t == Trigger.time.value)
            {
                this.value = GetSubjectValue(t, ally) * (t.value > 0 ? t.value : 1);
            }
            else if (t.t == Trigger.time.buff)
            {
                int subject = GetSubjectValue(t, ally);
                this.buff = new Vector2Int(
                    (int)(t.stats.x * subject),
                    (int)(t.stats.y * subject));
            }
        }
    }
    int GetSubjectValue(Trigger t, bool ally)
    {// ide jönnek amiket megkell majd adni hozzá pl kártya 
        switch (t.sub)
        {
            case Trigger.subject.HandCount:
                bool wantsOwnHand = t.tar != Trigger.Target.enemy;
                return GameManager.instance.GetHandCount(wantsOwnHand ? ally : !ally);
            case Trigger.subject.RemainingMana:
                UnityEngine.Debug.Log(" RemainingManA!!!! ");
                return playerController.GetRemainingMana()*t.multiValue;
        }
        return -888;// elvileg sose jutunk ide


    }
}
[System.Serializable]
public struct ClientEvent
{
    public ushort effectType;
    public ushort[] targetIds; // Tömb, mert így több célpontot is lefedhet egyetlen esemény
    public int value;
    public ushort doerId;
    public int[] newValues;
}