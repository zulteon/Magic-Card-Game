
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CsvImporterToScObj
{
    // =========================
    // BEÁLLÍTÁSOK
    // =========================

    public const string CSV_PATH = "Assets/Real_Cards/cards.csv";

    private const string CARD_FOLDER =
        "Assets/Real_Cards/Cards";

    private const string MINION_FOLDER =
        "Assets/Real_Cards/Minions";

    private const string SPELL_FOLDER =
        "Assets/Real_Cards/Spells";


    // CSV oszlopok:
    //x
    // 0 = ID
    // 1 = description elsõ fele
    // 2 = sprite fájlnév
    // 3 = description folytatása

    private const int ID_COLUMN = 0;
    private const int DESCRIPTION_COLUMN = 1;
    private const int SPRITE_COLUMN = 2;
    private const int DESCRIPTION_CONTINUE_COLUMN = 3;


    [MenuItem("Tools/Cards/Import Cards From CSV")]
    public static void ImportCards()
    {
        if (!File.Exists(CSV_PATH))
        {
            Debug.LogError(
                "Nem találom a CSV-t itt: " + CSV_PATH
            );

            return;
        }


        EnsureFolder("Assets/GeneratedCards");
        EnsureFolder(CARD_FOLDER);
        EnsureFolder(MINION_FOLDER);
        EnsureFolder(SPELL_FOLDER);


        string[] lines = File.ReadAllLines(
            CSV_PATH,
            Encoding.UTF8
        );


        int imported = 0;

        HashSet<ushort> usedIds =
            new HashSet<ushort>();


        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;


            char delimiter = DetectDelimiter(line);

            List<string> columns =
                ParseCsvLine(line, delimiter);


            if (columns.Count == 0)
                continue;


            // Elsõ oszlop.
            // BOM eltávolítás is, ha Excel rakott bele.
            string idText = GetColumn(
                columns,
                ID_COLUMN
            )
            .Trim()
            .Trim('\uFEFF');


            // Ha az elsõ cella nem szám,
            // akkor fejléc / fõcím / üres sor.
            if (!ushort.TryParse(idText, out ushort id))
                continue;


            if (!usedIds.Add(id))
            {
                Debug.LogError(
                    $"Duplikált card ID a CSV-ben: {id}"
                );

                continue;
            }


            string description1 =
                GetColumn(
                    columns,
                    DESCRIPTION_COLUMN
                ).Trim();


            string spriteName =
                GetColumn(
                    columns,
                    SPRITE_COLUMN
                ).Trim();


            string description2 =
                GetColumn(
                    columns,
                    DESCRIPTION_CONTINUE_COLUMN
                ).Trim();


            string fullDescription =
                CombineDescription(
                    description1,
                    description2
                );


            bool isSpell = id > 999;


            if (isSpell)
            {
                ImportSpell(
                    id,
                    spriteName,
                    fullDescription
                );
            }
            else
            {
                ImportMinion(
                    id,
                    spriteName,
                    fullDescription
                );
            }


            imported++;
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        Debug.Log(
            $"Import kész. {imported} kártya feldolgozva."
        );
    }


    // =========================================================
    // MINION
    // =========================================================

    private static void ImportMinion(
        ushort id,
        string spriteName,
        string description
    )
    {
        string minionPath =
            $"{MINION_FOLDER}/Minion_{id}.asset";

        string cardPath =
            $"{CARD_FOLDER}/Card_{id}.asset";


        // -------------------------
        // MinionData
        // -------------------------

        MinionData minion =
            AssetDatabase.LoadAssetAtPath<MinionData>(
                minionPath
            );


        if (minion == null)
        {
            minion =
                ScriptableObject.CreateInstance<MinionData>();

            AssetDatabase.CreateAsset(
                minion,
                minionPath
            );
        }


        minion.sprite = spriteName;
        minion.description = description;

        EditorUtility.SetDirty(minion);


        // -------------------------
        // Card
        // -------------------------

        Card card =
            AssetDatabase.LoadAssetAtPath<Card>(
                cardPath
            );


        if (card == null)
        {
            card =
                ScriptableObject.CreateInstance<Card>();

            AssetDatabase.CreateAsset(
                card,
                cardPath
            );
        }


        card.cardId = id;

        card.description = description;

        card.m = minion;
        card.spell = null;


        EditorUtility.SetDirty(card);


        Debug.Log(
            $"Minion importálva: {id} - {spriteName}"
        );
    }


    // =========================================================
    // SPELL
    // =========================================================

    private static void ImportSpell(
        ushort id,
        string spriteName,
        string description
    )
    {
        string spellPath =
            $"{SPELL_FOLDER}/Spell_{id}.asset";

        string cardPath =
            $"{CARD_FOLDER}/Card_{id}.asset";


        // -------------------------
        // Spell
        // -------------------------

        Spell spell =
            AssetDatabase.LoadAssetAtPath<Spell>(
                spellPath
            );


        if (spell == null)
        {
            spell =
                ScriptableObject.CreateInstance<Spell>();

            AssetDatabase.CreateAsset(
                spell,
                spellPath
            );
        }


        spell.sprite = spriteName;
        spell.description = description;

        EditorUtility.SetDirty(spell);


        // -------------------------
        // Card
        // -------------------------

        Card card =
            AssetDatabase.LoadAssetAtPath<Card>(
                cardPath
            );


        if (card == null)
        {
            card =
                ScriptableObject.CreateInstance<Card>();

            AssetDatabase.CreateAsset(
                card,
                cardPath
            );
        }


        card.cardId = id;

        card.description = description;

        card.spell = spell;
        card.m = null;


        EditorUtility.SetDirty(card);


        Debug.Log(
            $"Spell importálva: {id} - {spriteName}"
        );
    }


    // =========================================================
    // DESCRIPTION
    // =========================================================

    private static string CombineDescription(
        string first,
        string second
    )
    {
        if (string.IsNullOrWhiteSpace(first))
            return second;

        if (string.IsNullOrWhiteSpace(second))
            return first;


        return first.TrimEnd()
               + " "
               + second.TrimStart();
    }


    // =========================================================
    // CSV SEGÉDFÜGGVÉNYEK
    // =========================================================

    private static string GetColumn(
        List<string> columns,
        int index
    )
    {
        if (index < 0 || index >= columns.Count)
            return "";

        return columns[index];
    }


    private static char DetectDelimiter(string line)
    {
        int commas = 0;
        int semicolons = 0;

        bool insideQuotes = false;


        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];


            if (c == '"')
            {
                // "" egy idézõjel a szövegben
                if (
                    insideQuotes &&
                    i + 1 < line.Length &&
                    line[i + 1] == '"'
                )
                {
                    i++;
                    continue;
                }

                insideQuotes = !insideQuotes;
                continue;
            }


            if (insideQuotes)
                continue;


            if (c == ',')
                commas++;

            if (c == ';')
                semicolons++;
        }


        // Magyar Excel gyakran ; jelet használ
        return semicolons > commas
            ? ';'
            : ',';
    }


    private static List<string> ParseCsvLine(
        string line,
        char delimiter
    )
    {
        List<string> result =
            new List<string>();

        StringBuilder current =
            new StringBuilder();

        bool insideQuotes = false;


        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];


            if (c == '"')
            {
                if (
                    insideQuotes &&
                    i + 1 < line.Length &&
                    line[i + 1] == '"'
                )
                {
                    current.Append('"');
                    i++;

                    continue;
                }


                insideQuotes = !insideQuotes;

                continue;
            }


            if (c == delimiter && !insideQuotes)
            {
                result.Add(
                    current.ToString()
                );

                current.Clear();

                continue;
            }


            current.Append(c);
        }


        result.Add(
            current.ToString()
        );


        return result;
    }
    private const string STATS_CSV_PATH =
    "Assets/Real_Cards/costattack.csv";

    [MenuItem("Tools/Cards/Import Cost Attack Health")]
    public static void ImportCostAttackHealth()
    {
        if (!File.Exists(STATS_CSV_PATH))
        {
            Debug.LogError(
                "Nem találom a stat CSV-t itt: " + STATS_CSV_PATH
            );

            return;
        }

        string[] lines = File.ReadAllLines(
            STATS_CSV_PATH,
            Encoding.UTF8
        );

        int imported = 0;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            char delimiter = DetectDelimiter(line);

            List<string> columns =
                ParseCsvLine(line, delimiter);

            if (columns.Count == 0)
                continue;


            // -------------------------
            // ID
            // -------------------------

            string idText =
                GetColumn(columns, 0)
                .Trim()
                .Trim('\uFEFF');

            // Fejléc / üres / hibás sor.
            if (!ushort.TryParse(idText, out ushort id))
                continue;


            // -------------------------
            // Card betöltése
            // -------------------------

            string cardPath =
                $"{CARD_FOLDER}/Card_{id}.asset";

            Card card =
                AssetDatabase.LoadAssetAtPath<Card>(
                    cardPath
                );

            if (card == null)
            {
                Debug.LogWarning(
                    $"Nincs Card_{id}.asset, kihagyva."
                );

                continue;
            }


            // CSV:
            // 0 = ID
            // 1 = Name
            // 2 = Type / megjegyzés
            // 3 = Cost
            // 4 = Attack
            // 5 = Health


            // -------------------------
            // COST
            // -------------------------

            string costText =
                GetColumn(columns, 3).Trim();

            if (!string.IsNullOrWhiteSpace(costText))
            {
                if (int.TryParse(costText, out int cost))
                {
                    card.Cost = cost;
                }
                else
                {
                    Debug.LogWarning(
                        $"Hibás Cost érték ID:{id} -> '{costText}'"
                    );
                }
            }


            // -------------------------
            // MINION STATOK
            // -------------------------

            if (card.m != null)
            {
                string attackText =
                    GetColumn(columns, 4).Trim();

                string healthText =
                    GetColumn(columns, 5).Trim();


                if (!string.IsNullOrWhiteSpace(attackText))
                {
                    if (int.TryParse(
                        attackText,
                        out int attack
                    ))
                    {
                        card.m.attack = attack;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Hibás Attack érték ID:{id} -> '{attackText}'"
                        );
                    }
                }


                if (!string.IsNullOrWhiteSpace(healthText))
                {
                    if (int.TryParse(
                        healthText,
                        out int health
                    ))
                    {
                        card.m.health = health;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Hibás Health érték ID:{id} -> '{healthText}'"
                        );
                    }
                }


                EditorUtility.SetDirty(card.m);
            }


            // -------------------------
            // CARD MENTÉS
            // -------------------------

            EditorUtility.SetDirty(card);

            imported++;

            Debug.Log(
                $"Stat import: ID:{id} " +
                $"Cost:{card.Cost}" +
                (card.m != null
                    ? $" Attack:{card.m.attack} Health:{card.m.health}"
                    : " [SPELL]")
            );
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Cost/Attack/Health import kész. {imported} kártya feldolgozva."
        );
    }
    [MenuItem("Tools/Cards/Remove Sprite Extensions")]
    public static void RemoveSpriteExtensions()
    {
        int changed = 0;

        // -------------------------
        // MINIONOK
        // -------------------------

        string[] minionGuids =
            AssetDatabase.FindAssets(
                "t:MinionData",
                new[] { MINION_FOLDER }
            );

        foreach (string guid in minionGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            MinionData minion =
                AssetDatabase.LoadAssetAtPath<MinionData>(path);

            if (minion == null)
                continue;

            string cleaned =
                RemoveImageExtension(minion.sprite);

            if (cleaned != minion.sprite)
            {
                Debug.Log(
                    $"Sprite javítva: '{minion.sprite}' -> '{cleaned}'"
                );

                minion.sprite = cleaned;

                EditorUtility.SetDirty(minion);

                changed++;
            }
        }


        // -------------------------
        // SPELLEK
        // -------------------------

        string[] spellGuids =
            AssetDatabase.FindAssets(
                "t:Spell",
                new[] { SPELL_FOLDER }
            );

        foreach (string guid in spellGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Spell spell =
                AssetDatabase.LoadAssetAtPath<Spell>(path);

            if (spell == null)
                continue;

            string cleaned =
                RemoveImageExtension(spell.sprite);

            if (cleaned != spell.sprite)
            {
                Debug.Log(
                    $"Sprite javítva: '{spell.sprite}' -> '{cleaned}'"
                );

                spell.sprite = cleaned;

                EditorUtility.SetDirty(spell);

                changed++;
            }
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Sprite extension takarítás kész. {changed} asset módosítva."
        );
    }


    private static string RemoveImageExtension(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string result = value.Trim();

        string lower = result.ToLowerInvariant();

        if (lower.EndsWith(".jpeg"))
            return result.Substring(0, result.Length - 5);

        if (
            lower.EndsWith(".jpg") ||
            lower.EndsWith(".png") ||
            lower.EndsWith(".webp")
        )
        {
            return result.Substring(0, result.Length - 4);
        }

        return result;
    }
    private const string NAMES_CSV_PATH =
    "Assets/Real_Cards/names.csv";

    [MenuItem("Tools/Cards/Rename Assets From Names CSV")]
    public static void RenameAssetsFromNamesCsv()
    {
        if (!File.Exists(NAMES_CSV_PATH))
        {
            Debug.LogError(
                "Nem találom a names CSV-t itt: " + NAMES_CSV_PATH
            );

            return;
        }

        string[] lines = File.ReadAllLines(
            NAMES_CSV_PATH,
            Encoding.UTF8
        );

        int renamed = 0;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            char delimiter = DetectDelimiter(line);

            List<string> columns =
                ParseCsvLine(line, delimiter);

            if (columns.Count < 2)
                continue;


            // -------------------------
            // ID
            // -------------------------

            string idText =
                GetColumn(columns, 0)
                .Trim()
                .Trim('\uFEFF');

            // Fejléc / hibás sor.
            if (!ushort.TryParse(idText, out ushort id))
                continue;


            // -------------------------
            // NAME
            // -------------------------

            string newName =
                GetColumn(columns, 1).Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                Debug.LogWarning(
                    $"ID:{id} név nélkül, kihagyva."
                );

                continue;
            }


            // Unity fájlnévben problémás karakterek takarítása.
            newName = MakeSafeAssetName(newName);


            // -------------------------
            // CARD
            // -------------------------

            string cardPath =
                $"{CARD_FOLDER}/Card_{id}.asset";

            Card card =
                AssetDatabase.LoadAssetAtPath<Card>(
                    cardPath
                );

            if (card != null)
            {
                string error =
                    AssetDatabase.RenameAsset(
                        cardPath,
                        newName
                    );

                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log(
                        $"Card_{id} -> {newName}"
                    );

                    renamed++;
                }
                else
                {
                    Debug.LogError(
                        $"Card rename hiba ID:{id}: {error}"
                    );
                }
            }


            // -------------------------
            // MINION / SPELL
            // -------------------------

            bool isSpell = id > 999;

            string dataPath =
                isSpell
                    ? $"{SPELL_FOLDER}/Spell_{id}.asset"
                    : $"{MINION_FOLDER}/Minion_{id}.asset";


            UnityEngine.Object data =
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    dataPath
                );

            if (data != null)
            {
                string error =
                    AssetDatabase.RenameAsset(
                        dataPath,
                        newName
                    );

                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log(
                        $"{(isSpell ? "Spell" : "Minion")}_{id} -> {newName}"
                    );

                    renamed++;
                }
                else
                {
                    Debug.LogError(
                        $"Data rename hiba ID:{id}: {error}"
                    );
                }
            }
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Átnevezés kész. {renamed} asset átnevezve."
        );
    }


    private static string MakeSafeAssetName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string result = value.Trim();

        char[] invalidChars =
            Path.GetInvalidFileNameChars();

        foreach (char c in invalidChars)
        {
            result =
                result.Replace(c.ToString(), "");
        }

        return result.Trim();
    }
    [MenuItem("Tools/Cards/Restore Minion Spell ID Names")]
    public static void RestoreMinionSpellIdNames()
    {
        string[] cardGuids =
            AssetDatabase.FindAssets(
                "t:Card",
                new[] { CARD_FOLDER }
            );

        int renamed = 0;

        foreach (string guid in cardGuids)
        {
            string cardPath =
                AssetDatabase.GUIDToAssetPath(guid);

            Card card =
                AssetDatabase.LoadAssetAtPath<Card>(
                    cardPath
                );

            if (card == null)
                continue;


            // -------------------------
            // MINION
            // -------------------------

            if (card.m != null)
            {
                string minionPath =
                    AssetDatabase.GetAssetPath(card.m);

                string newName =
                    $"Minion_{card.cardId}";

                string error =
                    AssetDatabase.RenameAsset(
                        minionPath,
                        newName
                    );

                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log(
                        $"{card.m.name} -> {newName}"
                    );

                    renamed++;
                }
                else
                {
                    Debug.LogError(
                        $"Minion rename hiba ID:{card.cardId}: {error}"
                    );
                }
            }


            // -------------------------
            // SPELL
            // -------------------------

            if (card.spell != null)
            {
                string spellPath =
                    AssetDatabase.GetAssetPath(card.spell);

                string newName =
                    $"Spell_{card.cardId}";

                string error =
                    AssetDatabase.RenameAsset(
                        spellPath,
                        newName
                    );

                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log(
                        $"{card.spell.name} -> {newName}"
                    );

                    renamed++;
                }
                else
                {
                    Debug.LogError(
                        $"Spell rename hiba ID:{card.cardId}: {error}"
                    );
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"Quick fix kész. {renamed} Minion/Spell asset visszanevezve."
        );
    }
    // =========================================================
    // FOLDER
    // =========================================================

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;


        string parent =
            Path.GetDirectoryName(path)
                ?.Replace("\\", "/");

        string folder =
            Path.GetFileName(path);


        if (
            !string.IsNullOrEmpty(parent) &&
            !AssetDatabase.IsValidFolder(parent)
        )
        {
            EnsureFolder(parent);
        }


        AssetDatabase.CreateFolder(
            parent,
            folder
        );
    }
}