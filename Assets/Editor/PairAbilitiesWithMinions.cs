using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FillMissingMinionEffects
{
    private const string MINION_FOLDER =
        "Assets/Real_Cards/Minions";

    private const string ABILITY_FOLDER =
        "Assets/Real_Cards/Abilities";
    [MenuItem("Tools/Cards/Fill Missing Minion Effects")]
    public static void Run()
    {
        // Összes Effect előre betöltése.
        string[] effectGuids =
            AssetDatabase.FindAssets(
                "t:Effect",
                new[] { ABILITY_FOLDER }
            );

        List<Effect> effects = new();

        foreach (string guid in effectGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Effect effect =
                AssetDatabase.LoadAssetAtPath<Effect>(path);

            if (effect != null)
                effects.Add(effect);
        }


        string[] minionGuids =
            AssetDatabase.FindAssets(
                "t:MinionData",
                new[] { MINION_FOLDER }
            );


        int filled = 0;
        int alreadyHadEffect = 0;
        int notFound = 0;


        foreach (string guid in minionGuids)
        {
            string minionPath =
                AssetDatabase.GUIDToAssetPath(guid);

            MinionData minion =
                AssetDatabase.LoadAssetAtPath<MinionData>(
                    minionPath
                );

            if (minion == null)
                continue;


            // Már van legalább egy valódi effekt.
            if (
                minion.e != null &&
                minion.e.Any(x => x != null)
            )
            {
                alreadyHadEffect++;
                continue;
            }


            // Pl. Minion_120.asset -> 120
            string fileName =
                Path.GetFileNameWithoutExtension(
                    minionPath
                );

            if (!TryGetId(fileName, out int id))
            {
                Debug.LogWarning(
                    $"Nem tudtam ID-t kiolvasni: {fileName}"
                );

                continue;
            }


            List<Effect> matches =
                effects
                    .Where(effect =>
                        IsEffectForId(
                            effect.name,
                            id
                        )
                    )
                    .OrderBy(effect => effect.name)
                    .ToList();


            if (matches.Count == 0)
            {
                Debug.LogWarning(
                    $"Nincs Effect Minion_{id}-hez."
                );

                notFound++;
                continue;
            }


            if (minion.e == null)
                minion.e = new List<Effect>();
            else
                minion.e.Clear();


            minion.e.AddRange(matches);

            EditorUtility.SetDirty(minion);

            filled++;


            Debug.Log(
                $"Minion_{id}: {matches.Count} effect hozzáadva: " +
                string.Join(
                    ", ",
                    matches.Select(x => x.name)
                )
            );
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        Debug.Log(
            $"KÉSZ | " +
            $"Kitöltve: {filled} | " +
            $"Már volt effect: {alreadyHadEffect} | " +
            $"Nem talált: {notFound}"
        );
    }


    [MenuItem("Tools/Cards/Count Minions Without Effects")]
    public static void CountMinionsWithoutEffects()
    {
        string[] minionGuids =
            AssetDatabase.FindAssets(
                "t:MinionData",
                new[] { MINION_FOLDER }
            );

        int total = 0;
        int withoutEffect = 0;

        List<string> missing = new();

        foreach (string guid in minionGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            MinionData minion =
                AssetDatabase.LoadAssetAtPath<MinionData>(path);

            if (minion == null)
                continue;

            total++;

            bool hasEffect =
                minion.e != null &&
                minion.e.Any(x => x != null);

            if (hasEffect)
                continue;

            withoutEffect++;

            missing.Add(
                Path.GetFileNameWithoutExtension(path)
            );
        }

        Debug.Log(
            $"MINION EFFECT CHECK | " +
            $"Összes minion: {total} | " +
            $"Effect nélkül: {withoutEffect} | " +
            $"Effecttel: {total - withoutEffect}"
        );

        if (missing.Count > 0)
        {
            Debug.Log(
                "Effect nélküli minionok:\n" +
                string.Join("\n", missing)
            );
        }
    }

    [MenuItem("Tools/Cards/Fill Missing Spell Effects")]
    public static void FillMissingSpellEffects()
    {
        string[] effectGuids =
            AssetDatabase.FindAssets(
                "t:Effect",
                new[] { SPELL_ABILITY_FOLDER }
            );

        List<Effect> effects = new();

        foreach (string guid in effectGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Effect effect =
                AssetDatabase.LoadAssetAtPath<Effect>(path);

            if (effect != null)
                effects.Add(effect);
        }


        string[] spellGuids =
            AssetDatabase.FindAssets(
                "t:Spell",
                new[] { SPELL_FOLDER }
            );


        int filled = 0;
        int alreadyHadEffect = 0;
        int notFound = 0;


        foreach (string guid in spellGuids)
        {
            string spellPath =
                AssetDatabase.GUIDToAssetPath(guid);

            Spell spell =
                AssetDatabase.LoadAssetAtPath<Spell>(spellPath);

            if (spell == null)
                continue;


            // Már van legalább egy valódi effect.
            if (
                spell.e != null &&
                spell.e.Any(x => x != null)
            )
            {
                alreadyHadEffect++;
                continue;
            }


            // Pl. Spell_120.asset -> 120
            string fileName =
                Path.GetFileNameWithoutExtension(spellPath);

            if (!TryGetId(fileName, out int id))
            {
                Debug.LogWarning(
                    $"Nem tudtam ID-t kiolvasni: {fileName}"
                );

                continue;
            }


            List<Effect> matches =
                effects
                    .Where(effect =>
                        IsEffectForId(effect.name, id)
                    )
                    .OrderBy(effect => effect.name)
                    .ToList();


            if (matches.Count == 0)
            {
                Debug.LogWarning(
                    $"Nincs Effect Spell_{id}-hez."
                );

                notFound++;
                continue;
            }


            if (spell.e == null)
                spell.e = new List<Effect>();
            else
                spell.e.Clear();


            spell.e.AddRange(matches);

            EditorUtility.SetDirty(spell);

            filled++;


            Debug.Log(
                $"Spell_{id}: {matches.Count} effect hozzáadva: " +
                string.Join(
                    ", ",
                    matches.Select(x => x.name)
                )
            );
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        Debug.Log(
            $"SPELL EFFECT FILL KÉSZ | " +
            $"Kitöltve: {filled} | " +
            $"Már volt effect: {alreadyHadEffect} | " +
            $"Nem talált: {notFound}"
        );
    }
    private const string SPELL_FOLDER =
    "Assets/Real_Cards/Spells";

    private const string SPELL_ABILITY_FOLDER =
        "Assets/Real_Cards/Abilities_Spell";

    [MenuItem("Tools/Cards/Count Spells Without Effects")]
    public static void CountSpellsWithoutEffects()
    {
        string[] spellGuids =
            AssetDatabase.FindAssets(
                "t:Spell",
                new[] { SPELL_FOLDER }
            );

        int total = 0;
        int withoutEffect = 0;

        List<string> missing = new();
        List<int> missingIds = new();

        foreach (string guid in spellGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Spell spell =
                AssetDatabase.LoadAssetAtPath<Spell>(path);

            if (spell == null)
                continue;

            total++;

            bool hasEffect =
                spell.e != null &&
                spell.e.Any(x => x != null);

            if (hasEffect)
                continue;

            withoutEffect++;

            string fileName =
                Path.GetFileNameWithoutExtension(path);

            missing.Add(fileName);

            // Spell_120 -> 120
            if (TryGetId(fileName, out int id))
                missingIds.Add(id);
        }

        Debug.Log(
            $"SPELL EFFECT CHECK | " +
            $"Összes spell: {total} | " +
            $"Effect nélkül: {withoutEffect} | " +
            $"Effecttel: {total - withoutEffect}"
        );

        if (missing.Count > 0)
        {
            Debug.Log(
                "Effect nélküli spellek:\n" +
                string.Join("\n", missing)
            );

            // KÜLÖN console bejegyzés
            Debug.Log(
                "HIÁNYZÓ SPELL ID-K:\n" +
                string.Join(", ", missingIds.OrderBy(x => x))
            );
        }
    }

    // =========================================================
    // Minion_120 -> 120
    // =========================================================

    private static bool TryGetId(
        string fileName,
        out int id
    )
    {
        id = 0;

        int underscore =
            fileName.LastIndexOf('_');

        if (
            underscore < 0 ||
            underscore >= fileName.Length - 1
        )
            return false;


        string idText =
            fileName.Substring(
                underscore + 1
            );


        return int.TryParse(
            idText,
            out id
        );
    }


    // =========================================================
    // ID MATCH
    //
    // 120:
    //
    // _120             ✓
    // _120_damage      ✓
    // _120OnDeath      ✓
    //
    // _1200            X
    // _1201_test       X
    //
    // Így pl. ID 12 nem fogja elkapni a 120-as effectet.
    // =========================================================

    private static bool IsEffectForId(
        string effectName,
        int id
    )
    {
        string prefix =
            "_" + id;

        if (!effectName.StartsWith(prefix))
            return false;


        if (effectName.Length == prefix.Length)
            return true;


        // Az ID után már nem jöhet újabb számjegy.
        char next =
            effectName[prefix.Length];

        return !char.IsDigit(next);
    }
}