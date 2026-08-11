
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