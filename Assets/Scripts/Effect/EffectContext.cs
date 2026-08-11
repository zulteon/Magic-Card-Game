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
        }
        this.effect = e;
        this.doerId = doerId;
        this.value = e.value + extraValue;
        this.targetIds = targetIds.ToArray();
        /*
          if (e.toHand) Effekthez megcsinálni mikro optim.
        {
            this.targets = Array.Empty<MinionLogic>();
            return;
        }
         */
        // Egy iteráció temp tömbbel
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
            else UnityEngine.Debug.Log($"<color=red> whaat </color> {targetIds[i]}");
        }

        // Pontos méret
        this.targets = new MinionLogic[count];
        System.Array.Copy(temp, this.targets, count);
    }

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