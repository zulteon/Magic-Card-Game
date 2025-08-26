using System.Collections.Generic;
using UnityEngine;

public class EffectManagerServer : MonoBehaviour
{
    Dictionary<ushort, LiveEffect> effects=new();
    public static EffectManagerServer instance { get; set; }
    void Start()
    {
        if (instance == null)
            instance = this;
        else
            if (instance != this)
            Destroy(this);
    }
    public EffectManagerServer(LiveEffect effect,ushort id) { effects[id] = effect; }
    public void RemoveEffect(ushort id) {effects.Remove(id); }
}
