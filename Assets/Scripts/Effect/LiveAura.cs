using System.Collections.Generic;
using UnityEngine;

public class LiveAura : MonoBehaviour
{
    public Dictionary<LiveEffectData, int> liveAura=new(); // how many turns left
    public Dictionary<LiveEffectData, ushort> liveAuraOnMinion=new();  // what is the minion id

    public void AddAura(LiveEffectData data, ushort minionId)
    {
        liveAuraOnMinion.Add(data, minionId);
    }
    public void  AddAura(LiveEffectData data, int turn)
    {
        liveAura.Add(data, turn);
    }
}

[System.Serializable]
public class LiveEffectData // Csak adat!
{
    public ushort effectId;
    public ushort targetId;
    public ushort creatorId;
    public int value;
    public int duration;
}
/*
 Ha megfordítod — az ID a kulcs, az adat az érték —, akkor a lekérés egy sorból megvan:
csharppublic Dictionary<ushort, LiveEffectData> liveAura = new();
// ...
if (liveAura.TryGetValue(requestedId, out var data)) { ... }*/