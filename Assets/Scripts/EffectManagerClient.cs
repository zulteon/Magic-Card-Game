using System.Collections.Generic;
using UnityEngine;

public class EffectManagerClient : MonoBehaviour
{
    // Ez a dictionary t�rolja az �sszes statikus effekt adatot.
    private static Dictionary<ushort, Effect> effectRegistry = new ();
    public static EffectManagerClient instance { get; private set; }

    // Ebbe a list�ba kell a Unity editorban bet�lteni az �sszes EffectData ScriptableObjectet.
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

    // Ezzel a met�dussal k�rheted le az effekt adatait az ID alapj�n.
    public  Effect GetEffectData(ushort id)
    {
        return effectRegistry.TryGetValue(id, out Effect effect) ? effect : null;
    }
}