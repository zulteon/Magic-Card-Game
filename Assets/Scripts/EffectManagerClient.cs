using System.Collections.Generic;
using UnityEngine;

public class EffectManagerClient : MonoBehaviour
{
    // Ez a dictionary tárolja az összes statikus effekt adatot.
    private static Dictionary<ushort, Effect> effectRegistry = new ();
    public static EffectManagerClient instance { get; private set; }

    // Ebbe a listába kell a Unity editorban betölteni az összes EffectData ScriptableObjectet.
    [SerializeField]
    private List<Effect> allEffects;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialize();
        }
        else
        {
            if (instance != this)
                Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        foreach (Effect effect in allEffects)
        {
            if (effectRegistry.ContainsKey(effect.effectId))
            {
                Debug.LogError($"Duplicate effect ID found: {effect.effectId}");
            }
            else
            {
                effectRegistry.Add(effect.effectId, effect);
            }
        }
    }

    // Ezzel a metódussal kérheted le az effekt adatait az ID alapján.
    public  Effect GetEffectData(ushort id)
    {
        return effectRegistry.TryGetValue(id, out Effect effect) ? effect : null;
    }
}