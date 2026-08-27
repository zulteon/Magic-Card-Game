using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameKit.Dependencies.Utilities;
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
#if UNITY_EDITOR
            LoadFromDirectory();
#endif
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
            if (effect.type == Effect.Type.none)
                Debug.LogWarning($"[{effect.name}] típusa None — nem fog lefutni!");

            if (effect.triggers == null || effect.triggers.Length == 0)
                Debug.LogWarning($"[{effect.name}] nincs triggere! :D ");
            else
                for (int i = 0; i < effect.triggers.Length; i++)
                    if (effect.triggers[i] == null)
                        Debug.LogError($"[{effect.name}] üres trigger a(z) {i}. helyen!");

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


    public List<Effect> GetEffectData(List<ushort> ids)
    {
        return ids?.Select(id => GetEffectData(id)).Where(e => e != null).ToList() ?? new List<Effect>();
    }


    //csak teszteléshez
#if UNITY_EDITOR
    [ContextMenu("Load effects from folder")]
    private void LoadFromDirectory()
    {
        allEffects = new List<Effect>();

        var guids = UnityEditor.AssetDatabase.FindAssets("t:Effect",
            new[] { "Assets/Real_Cards/" });

        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var e = UnityEditor.AssetDatabase.LoadAssetAtPath<Effect>(path);
            if (e != null) allEffects.Add(e);
        }

        allEffects.Sort((a, b) => a.effectId.CompareTo(b.effectId));
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"{allEffects.Count} effekt betöltve.");
    }
#endif
}