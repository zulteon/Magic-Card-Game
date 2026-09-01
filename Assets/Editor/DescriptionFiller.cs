using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CardDescriptionImporter
{
    private const string CARD_FOLDER =
        "Assets/Real_Cards/cards";


    [MenuItem("Tools/Cards/Import Descriptions From CSV")]
    public static void ImportDescriptions()
    {
        string csvPath = EditorUtility.OpenFilePanel(
            "Description CSV kiválasztása",
            "",
            "csv"
        );

        if (string.IsNullOrEmpty(csvPath))
            return;


        // ==========================================
        // Card ID -> Card lookup
        // ==========================================

        Dictionary<ushort, Card> cards =
            new Dictionary<ushort, Card>();

        string[] cardGuids =
            AssetDatabase.FindAssets(
                "t:Card",
                new[] { CARD_FOLDER }
            );

        foreach (string guid in cardGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Card card =
                AssetDatabase.LoadAssetAtPath<Card>(path);

            if (card == null)
                continue;

            cards[card.cardId] = card;
        }


        // ==========================================
        // CSV
        // ==========================================

        string[] lines =
            File.ReadAllLines(csvPath);

        int updated = 0;
        int notFound = 0;
        int invalid = 0;
        int missingData = 0;

        List<ushort> notFoundIds =
            new List<ushort>();

        List<ushort> missingDataIds =
            new List<ushort>();


        foreach (string rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            string line =
                rawLine.Trim();


            // Csak az elsõ ; választja szét az ID-t
            // és a descriptiont.
            int separatorIndex =
                line.IndexOf(';');

            if (separatorIndex < 0)
            {
                Debug.LogWarning(
                    $"Hibás CSV sor: {line}"
                );

                invalid++;
                continue;
            }


            string idText =
                line.Substring(0, separatorIndex)
                    .Trim()
                    .Trim('\uFEFF');


            string description =
                line.Substring(separatorIndex + 1)
                    .Trim();


            // CSV idézõjelek kezelése
            if (
                description.Length >= 2 &&
                description.StartsWith("\"") &&
                description.EndsWith("\"")
            )
            {
                description =
                    description.Substring(
                        1,
                        description.Length - 2
                    );

                description =
                    description.Replace(
                        "\"\"",
                        "\""
                    );
            }


            // ==========================================
            // ID
            // ==========================================

            if (!ushort.TryParse(
                idText,
                out ushort id
            ))
            {
                Debug.LogWarning(
                    $"Hibás Card ID: {idText}"
                );

                invalid++;
                continue;
            }


            // ==========================================
            // Card keresés
            // ==========================================

            if (!cards.TryGetValue(
                id,
                out Card card
            ))
            {
                notFound++;
                notFoundIds.Add(id);

                continue;
            }


            // ==========================================
            // DESCRIPTION FELÜLÍRÁS
            // ==========================================

            if (id >= 1000)
            {
                // SPELL
                if (card.spell == null)
                {
                    missingData++;
                    missingDataIds.Add(id);

                    Debug.LogWarning(
                        $"Card {id} spell referencia NULL."
                    );

                    continue;
                }

                card.spell.description =
                    description;

                EditorUtility.SetDirty(
                    card.spell
                );
            }
            else
            {
                // MINION
                if (card.m == null)
                {
                    missingData++;
                    missingDataIds.Add(id);

                    Debug.LogWarning(
                        $"Card {id} minion referencia NULL."
                    );

                    continue;
                }

                card.m.description =
                    description;

                EditorUtility.SetDirty(
                    card.m
                );
            }


            updated++;

            Debug.Log(
                $"{id} description frissítve: " +
                description
            );
        }


        // ==========================================
        // Mentés
        // ==========================================

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();


        // ==========================================
        // ÖSSZEGZÉS
        // ==========================================

        Debug.Log(
            $"DESCRIPTION IMPORT KÉSZ | " +
            $"Frissítve: {updated} | " +
            $"Card nem található: {notFound} | " +
            $"Hiányzó Minion/Spell referencia: {missingData} | " +
            $"Hibás sor: {invalid}"
        );


        if (notFoundIds.Count > 0)
        {
            Debug.Log(
                "NEM TALÁLHATÓ CARD ID-K:\n" +
                string.Join(
                    ", ",
                    notFoundIds
                        .Distinct()
                        .OrderBy(x => x)
                )
            );
        }


        if (missingDataIds.Count > 0)
        {
            Debug.Log(
                "HIÁNYZÓ MINION/SPELL REFERENCIÁS ID-K:\n" +
                string.Join(
                    ", ",
                    missingDataIds
                        .Distinct()
                        .OrderBy(x => x)
                )
            );
        }
    }
}