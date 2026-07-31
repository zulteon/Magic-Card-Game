using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{// Valoszinüleg töröljük ezt a kodot az effectmanager client jó
    public static EffectManager Instance;

    [Header("Effect ScriptableObjects")]
    public List<Effect> allEffects = new List<Effect>();

    private Dictionary<int, Effect> effectLookup = new Dictionary<int, Effect>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            BuildEffectLookup();
        }
    }

    void BuildEffectLookup()
    {
        effectLookup.Clear();
        foreach (Effect effect in allEffects)
        {
            if (effect != null)
            {
                effectLookup[effect.GetInstanceID()] = effect; // vagy effect.id
            }
        }
    }

    public Effect GetEffectById(int id)
    {
        effectLookup.TryGetValue(id, out Effect effect);
        return effect;
    }
    public List<Effect> GetEffectById(List<int> ids)
    {
        List<Effect> effects = new List<Effect>();
        foreach(int i in ids)
            effects.Add(GetEffectById(i));
        return effects;
    }
}
