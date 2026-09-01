using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SpellCsvImporter
{
    private const string CARD_FOLDER =
        "Assets/Real_Cards/cards";


    [MenuItem("Tools/Cards/Import Spell CSV")]
    public static void ImportSpellCsv()
    {
        string csvPath = EditorUtility.OpenFilePanel(
            "Spell CSV kiválasztása",
            "",
            "csv"
        );

        if (string.IsNullOrEmpty(csvPath))
            return;


        // ==========================
        // Card lookup ID alapján
        // ==========================

        Dictionary<ushort, Card> cards = new();
        Dictionary<ushort, string> cardPaths = new();

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Card",
                new[] { CARD_FOLDER }
            );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Card card =
                AssetDatabase.LoadAssetAtPath<Card>(path);

            if (card == null)
                continue;

            cards[card.cardId] = card;
            cardPaths[card.cardId] = path;
        }


        // ==========================
        // CSV
        // ==========================

        string[] lines =
            File.ReadAllLines(csvPath);

        int updated = 0;
        int notFound = 0;
        int invalid = 0;

        List<ushort> missingIds = new();


        foreach (string rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            List<string> columns =
                ParseCsvLine(rawLine);

            if (columns.Count < 5)
            {
                // Header vagy hibás sor
                continue;
            }


            string idText =
                columns[0]
                    .Trim()
                    .Trim('\uFEFF');


            // Header:
            // ;Name;Cost;Description;Sprite
            if (!ushort.TryParse(
                idText,
                out ushort id
            ))
            {
                continue;
            }


            string cardName =
                columns[1].Trim();

            string costText =
                columns[2].Trim();

            string description =
                columns[3].Trim();

            string sprite =
                columns[4].Trim();


            if (!int.TryParse(
                costText,
                out int cost
            ))
            {
                Debug.LogWarning(
                    $"Hibás Cost ID {id}: {costText}"
                );

                invalid++;
                continue;
            }


            // ==========================
            // Card keresés
            // ==========================

            if (!cards.TryGetValue(id, out Card card))
            {
                card = CreateNewSpellCard(
                    id,
                    cardName,
                    cost,
                    description,
                    sprite
                );

                if (card == null)
                {
                    Debug.LogError($"Nem sikerült létrehozni a spell cardot: {id}");
                    continue;
                }

                cards[id] = card;

                updated++;

                Debug.Log(
                    $"ÚJ SPELL LÉTREHOZVA | " +
                    $"{id} | {cardName} | Cost: {cost}"
                );

                continue;
            }


            if (card.spell == null)
            {
                Debug.LogWarning(
                    $"Card {id} spell referenciája NULL."
                );

                invalid++;
                continue;
            }


            // ==========================
            // ADATOK FELÜLÍRÁSA
            // ==========================

            card.Cost = cost;

            card.spell.description =
                description;


            // .png/.jpg stb. levágása
            card.spell.sprite =
                Path.GetFileNameWithoutExtension(
                    sprite
                );


            // ==========================
            // CARD ASSET ÁTNEVEZÉSE
            // ==========================

            if (!string.IsNullOrWhiteSpace(cardName))
            {
                string cardPath =
                    cardPaths[id];

                string renameError =
                    AssetDatabase.RenameAsset(
                        cardPath,
                        cardName
                    );

                if (!string.IsNullOrEmpty(renameError))
                {
                    Debug.LogWarning(
                        $"Nem sikerült átnevezni ID {id}: " +
                        renameError
                    );
                }
            }


            EditorUtility.SetDirty(card);
            EditorUtility.SetDirty(card.spell);

            updated++;


            Debug.Log(
                $"SPELL {id} frissítve | " +
                $"{cardName} | " +
                $"Cost: {cost} | " +
                $"Sprite: {card.spell.sprite}"
            );
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        // ==========================
        // ÖSSZEGZÉS
        // ==========================

        Debug.Log(
            $"SPELL CSV IMPORT KÉSZ | " +
            $"Frissítve: {updated} | " +
            $"Nem található: {notFound} | " +
            $"Hibás: {invalid}"
        );


        if (missingIds.Count > 0)
        {
            missingIds.Sort();

            Debug.Log(
                "NEM TALÁLHATÓ SPELL CARD ID-K:\n" +
                string.Join(", ", missingIds)
            );
        }
    }


    // ==========================================
    // Egyszerû ; CSV parser
    // idézõjeleket is kezeli
    // ==========================================
    private static Card CreateNewSpellCard(
    ushort id,
    string cardName,
    int cost,
    string description,
    string sprite)
    {
        const string SPELL_FOLDER =
            "Assets/Real_Cards/Spells";

        const string CARD_FOLDER =
            "Assets/Real_Cards/cards";


        // .png levágása
        string spriteName =
            Path.GetFileNameWithoutExtension(sprite);


        // =====================================
        // SPELL ASSET
        // =====================================

        Spell spell =
            ScriptableObject.CreateInstance<Spell>();

        spell.sprite = spriteName;
        spell.description = description;
        spell.e = new List<Effect>();


        string spellPath =
            $"{SPELL_FOLDER}/Spell_{id}.asset";

        // Ha valamiért már van ilyen asset, ne írjuk felül.
        if (AssetDatabase.LoadAssetAtPath<Spell>(spellPath) != null)
        {
            Debug.LogWarning(
                $"Spell asset már létezik: {spellPath}"
            );

            spell =
                AssetDatabase.LoadAssetAtPath<Spell>(
                    spellPath
                );
        }
        else
        {
            AssetDatabase.CreateAsset(
                spell,
                spellPath
            );
        }


        // =====================================
        // CARD ASSET
        // =====================================

        Card card =
            ScriptableObject.CreateInstance<Card>();

        card.cardId = id;
        card.Cost = cost;

        // Ha a régi exporter még ezt is használja,
        // nem baj, ha itt is megvan.
        card.description = description;

        card.spell = spell;
        card.m = null;


        // A Card asset nálad emberi nevet kap.
        string safeName =
            MakeSafeFileName(cardName);

        string cardPath =
            $"{CARD_FOLDER}/{safeName}.asset";


        // Ha azonos nevû kártya már van:
        if (AssetDatabase.LoadAssetAtPath<Card>(cardPath) != null)
        {
            cardPath =
                $"{CARD_FOLDER}/{safeName}_{id}.asset";
        }


        AssetDatabase.CreateAsset(
            card,
            cardPath
        );


        EditorUtility.SetDirty(spell);
        EditorUtility.SetDirty(card);


        return card;
    }
    private static string MakeSafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name.Trim();
    }
    private static List<string> ParseCsvLine(
        string line
    )
    {
        List<string> result = new();

        string current = "";
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (
                    inQuotes &&
                    i + 1 < line.Length &&
                    line[i + 1] == '"'
                )
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (
                c == ';' &&
                !inQuotes
            )
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);

        return result;
    }
}