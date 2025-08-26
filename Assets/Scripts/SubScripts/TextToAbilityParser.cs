using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using UnityEngine;

public static class TextToAbilityParser
{
    // Reflection-alapú action keywords - automatikusan feltölti az Effect.Type enum értékeivel
    private static Dictionary<string, Effect.Type> _actionKeywords;
    private static Dictionary<string, Effect.Type> ActionKeywords
    {
        get
        {
            if (_actionKeywords == null)
            {
                InitializeActionKeywords();
            }
            return _actionKeywords;
        }
    }

    // Action keywords inicializálása reflection-nel
    private static void InitializeActionKeywords()
    {
        _actionKeywords = new Dictionary<string, Effect.Type>();

        // Effect.Type enum értékeinek lekérése
        var enumValues = Enum.GetValues(typeof(Effect.Type)).Cast<Effect.Type>();

        foreach (Effect.Type enumValue in enumValues)
        {
            string enumName = enumValue.ToString().ToLower();

            // Az enum név hozzáadása
            if (!_actionKeywords.ContainsKey(enumName))
            {
                _actionKeywords[enumName] = enumValue;
            }

            // Szinonimák hozzáadása reflection alapján
            AddSynonymsForAction(enumName, enumValue);
        }

        Debug.Log($"Initialized {_actionKeywords.Count} action keywords via reflection:");
        foreach (var kvp in _actionKeywords)
        {
            Debug.Log($"  '{kvp.Key}' -> {kvp.Value}");
        }
    }

    // Szinonimák hozzáadása az action-höz
    private static void AddSynonymsForAction(string enumName, Effect.Type enumValue)
    {
        // Szinonima szótár - ezt könnyû bõvíteni
        var synonyms = new Dictionary<string, string[]>
        {
            { "damage", new[] { "deal", "hurt", "strike", "hit" } },
            { "heal", new[] { "restore", "cure", "mend", "recover" } },
            { "give", new[] { "grant", "bestow", "provide", "add" } },
            { "none", new[] { "nothing", "empty", "null" } }
        };

        if (synonyms.ContainsKey(enumName))
        {
            foreach (string synonym in synonyms[enumName])
            {
                if (!_actionKeywords.ContainsKey(synonym))
                {
                    _actionKeywords[synonym] = enumValue;
                }
            }
        }
    }

    private static readonly Dictionary<string, Trigger.Target> TargetKeywords = new Dictionary<string, Trigger.Target>
    {
                                                         
        {    "minions",      Trigger.Target.all         },
        {    "characters",   Trigger.Target.all         },
        {    "hero",         Trigger.Target.self         },
        {    "minion",       Trigger.Target.self         },
        {    "allies",       Trigger.Target.self         },
        {    "ally",         Trigger.Target.self         },
        {    "heros",        Trigger.Target.all         },
        {    "left",         Trigger.Target.left        },
        {    "right",        Trigger.Target.left        },
        {    "adjacent",     Trigger.Target.adjacent    },
        {    "neighbours",   Trigger.Target.adjacent    },
        {    "neighbour",    Trigger.Target.adjacent    }
        };                   
        /*
        { "enemy", Trigger.Target.enemy },
        { "enemies", Trigger.Target.enemy },
        { "ally", Trigger.Target.ally },
        { "allies", Trigger.Target.ally },
        { "friendly", Trigger.Target.ally },
        { "hero", Trigger.Target.hero },
        { "your hero", Trigger.Target.hero },
        { "minion", Trigger.Target.minion },
        { "minions", Trigger.Target.minion },
        { "character", Trigger.Target.character },
        { "characters", Trigger.Target.character }*/
    

    private static readonly Dictionary<string, Effect.TargetCast> CastKeywords = new Dictionary<string, Effect.TargetCast>
    {
        { "all", Effect.TargetCast.all },
        { "random", Effect.TargetCast.random },
        { "a random", Effect.TargetCast.random },
        { "single", Effect.TargetCast.single },
        { "one", Effect.TargetCast.single },
        {    "two",       Effect.TargetCast.multi },
        {   "three",     Effect.TargetCast.multi },
        {   "four",      Effect.TargetCast.multi },
        {    "five",      Effect.TargetCast.multi },
        {    "six",       Effect.TargetCast.multi },
        {    "seven",     Effect.TargetCast.multi },
        {    "eight",      Effect.TargetCast.multi },
    };

    private static readonly Dictionary<string, Trigger.TargetType> TypeKeywords = new Dictionary<string, Trigger.TargetType>
    {
        { "minion", Trigger.TargetType.minion },
        { "minions", Trigger.TargetType.minion },
        { "hero", Trigger.TargetType.hero },
        { "character", Trigger.TargetType.character },
        { "characters", Trigger.TargetType.character },
        { "taunt", Trigger.TargetType.taunt },
         
    };

    /// <summary>
    /// Szöveges leírásból Effect objektumot hoz létre
    /// </summary>
    /// <param name="text">A szöveges leírás (pl: "Deal 5 damage to all enemies")</param>
    /// <returns>A megfelelõ Effect objektum, vagy null ha nem sikerült feldolgozni</returns>
    public static Effect ParseTextToEffect(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        text = text.ToLower().Trim();

        // Effect objektum létrehozása
        Effect effect = ScriptableObject.CreateInstance<Effect>();

        // Alapértelmezett értékek
        effect.type = Effect.Type.none;
        effect.targetCast = Effect.TargetCast.single;
        effect.target = Trigger.Target.enemy;
        effect.targetType = Trigger.TargetType.minion;
        effect.value = 0;

        try
        {
            // 1. Action típus meghatározása
            ParseActionType(text, effect);

            // 2. Érték kinyerése (számok)
            ParseValue(text, effect);

            // 3. Target cast meghatározása (all, random, stb.)
            ParseTargetCast(text, effect);

            // 4. Target meghatározása (enemy, ally, hero, stb.)
            ParseTarget(text, effect);

            // 5. Target type meghatározása (minion, hero, character)
            ParseTargetType(text, effect);

            return effect;
        }
        catch (Exception e)
        {
            Debug.LogError($"Hiba az effect feldolgozásakor: {e.Message}");
            return null;
        }
    }

    private static void ParseActionType(string text, Effect effect)
    {
        foreach (var keyword in ActionKeywords)
        {
            if (text.Contains(keyword.Key))
            {
                effect.type = keyword.Value;
                break;
            }
        }
    }

    private static void ParseValue(string text, Effect effect)
    {
        // Számok keresése a szövegben
        var matches = Regex.Matches(text, @"\d+");
        if (matches.Count > 0)
        {
            if (int.TryParse(matches[0].Value, out int value))
            {
                effect.value = value;
            }
        }
    }

    private static void ParseTargetCast(string text, Effect effect)
    {
        foreach (var keyword in CastKeywords)
        {
            if (text.Contains(keyword.Key))
            {
                effect.targetCast = keyword.Value;
                break;
            }
        }
    }

    private static void ParseTarget(string text, Effect effect)
    {
        // Specifikus kifejezések keresése (pl: "your hero")
      /*  if (text.Contains("your hero"))
        {
            effect.target = Trigger.Target.hero;
            return;
        }*/

        foreach (var keyword in TargetKeywords)
        {
            if (text.Contains(keyword.Key))
            {
                effect.target = keyword.Value;
                break;
            }
        }
    }

    private static void ParseTargetType(string text, Effect effect)
    {
        foreach (var keyword in TypeKeywords)
        {
            if (text.Contains(keyword.Key))
            {
                effect.targetType = keyword.Value;
                break;
            }
        }
    }

    /// <summary>
    /// Manually add new action synonym (runtime használatra)
    /// </summary>
    public static void AddActionSynonym(string keyword, Effect.Type actionType)
    {
        ActionKeywords[keyword.ToLower()] = actionType;
        Debug.Log($"Added new action synonym: '{keyword}' -> {actionType}");
    }

    /// <summary>
    /// Get all available action keywords (debug célokra)
    /// </summary>
    public static Dictionary<string, Effect.Type> GetAllActionKeywords()
    {
        return new Dictionary<string, Effect.Type>(ActionKeywords);
    }

    /// <summary>
    /// Force refresh action keywords (ha új enum értékeket adtál hozzá)
    /// </summary>
    public static void RefreshActionKeywords()
    {
        _actionKeywords = null;
        Debug.Log("Action keywords refreshed!");
    }
    public static Effect[] ParseMultipleEffects(string[] texts)
    {
        if (texts == null || texts.Length == 0)
            return new Effect[0];

        List<Effect> effects = new List<Effect>();

        foreach (string text in texts)
        {
            Effect effect = ParseTextToEffect(text);
            if (effect != null)
            {
                effects.Add(effect);
            }
        }

        return effects.ToArray();
    }

    /// <summary>
    /// Effect objektumot szöveggé alakít vissza (debug célokra)
    /// </summary>
    public static string EffectToText(Effect effect)
    {
        if (effect == null) return "Null effect";

        string action = effect.type.ToString();
        string value = effect.value.ToString();
        string cast = effect.targetCast.ToString();
        string target = effect.target.ToString();
        string type = effect.targetType.ToString();

        return $"{action.ToUpper()} {value} to {cast} {target} {type}";
    }

    /// <summary>
    /// Teszt függvény különbözõ szövegekkel + reflection teszt
    /// </summary>
    public static void RunTests()
    {
        string[] testTexts = {
            "Deal 5 damage to all enemies",
            "Heal 3 health to your hero",
            "Damage 2 to a random enemy minion",  // enum név használata
            "Restore 4 health to all friendly minions", // szinonima
            "Give 1 buff to all enemy characters",
            "Strike 3 damage to your hero" // új szinonima
        };

        Debug.Log("=== Text To Ability Parser Tests (with Reflection) ===");
        Debug.Log($"Available action keywords: {ActionKeywords.Count}");

        foreach (string text in testTexts)
        {
            Effect effect = ParseTextToEffect(text);
            if (effect != null)
            {
                string result = EffectToText(effect);
                Debug.Log($"Input: '{text}' -> Output: '{result}'");
                Debug.Log($"  Type: {effect.type}, Value: {effect.value}, Cast: {effect.targetCast}, Target: {effect.target}, TargetType: {effect.targetType}");
            }
            else
            {
                Debug.LogWarning($"Failed to parse: '{text}'");
            }
        }

        // Reflection info
        Debug.Log("\n=== Available Action Keywords ===");
        foreach (var kvp in ActionKeywords)
        {
            Debug.Log($"'{kvp.Key}' -> {kvp.Value}");
        }
    }
}