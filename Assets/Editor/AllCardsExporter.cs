using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class AllCardsExporter : EditorWindow
{
    private const string CARD_FOLDER =
        "Assets/Real_Cards/cards";

    private const string OUTPUT_FILE =
        "Assets/StreamingAssets/AllCards.json";

    private const string MINION_ABILITY_FOLDER =
    "Assets/Real_Cards/Abilities";

    private const string SPELL_ABILITY_FOLDER =
        "Assets/Real_Cards/Abilities_Spell";
    private string ignoredIdsText = "";

    private List<Card> foundCards = new();

    private Vector2 scroll;


    [MenuItem("Tools/Cards/Export AllCards")]
    public static void Open()
    {
        AllCardsExporter window =
            GetWindow<AllCardsExporter>();

        window.titleContent =
            new GUIContent("AllCards Export");

        window.minSize =
            new Vector2(420, 260);

        window.RefreshCards();
    }


    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "AllCards.json Export",
            EditorStyles.boldLabel
        );

        GUILayout.Space(8);


        EditorGUILayout.LabelField(
            $"Talált kártyák: {foundCards.Count}"
        );


        GUILayout.Space(12);


        EditorGUILayout.LabelField(
            "Kihagyandó Card ID-k"
        );

        ignoredIdsText =
            EditorGUILayout.TextField(
                ignoredIdsText
            );

        EditorGUILayout.HelpBox(
            "Vesszõvel elválasztva, pl.:\n" +
            "12, 48, 1016, 1028",
            MessageType.Info
        );


        GUILayout.Space(10);


        if (GUILayout.Button(
            "Kártyák újraolvasása",
            GUILayout.Height(30)
        ))
        {
            RefreshCards();
        }


        GUILayout.Space(5);


        if (GUILayout.Button(
            "AllCards.json elkészítése",
            GUILayout.Height(45)
        ))
        {
            Export();
        }


        GUILayout.Space(10);


        DrawCardInfo();
    }


    // =========================================================
    // KÁRTYÁK BEOLVASÁSA
    // =========================================================

    private void RefreshCards()
    {
        foundCards.Clear();

        string[] guids =
    AssetDatabase.FindAssets(
        "t:Card",
        new[]
        {
            CARD_FOLDER,
            MINION_ABILITY_FOLDER,
            SPELL_ABILITY_FOLDER
        }
    );


        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Card card =
                AssetDatabase.LoadAssetAtPath<Card>(
                    path
                );

            if (card != null)
                foundCards.Add(card);
        }


        foundCards =
            foundCards
                .OrderBy(c => c.cardId)
                .ToList();


        Repaint();


        Debug.Log(
            $"AllCards Exporter: " +
            $"{foundCards.Count} Card asset találva."
        );
    }


    // =========================================================
    // EXPORT
    // =========================================================

    private void Export()
    {
        
        HashSet<ushort> ignoredIds =
            ParseIgnoredIds();


        List<Card> cardsToExport =
            foundCards
                .Where(card =>
                    !ignoredIds.Contains(card.cardId)
                )
                .ToList();


        CardCollection collection =
            Deck.CardsToCollection(
                cardsToExport
            );


        string json =
            JsonConvert.SerializeObject(
                collection,
                Formatting.Indented
            );


        string directory =
            Path.GetDirectoryName(
                OUTPUT_FILE
            );


        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);


        File.WriteAllText(
            OUTPUT_FILE,
            json
        );


        AssetDatabase.Refresh();


        Debug.Log(
            $"ALLCARDS EXPORT KÉSZ | " +
            $"Talált: {foundCards.Count} | " +
            $"Kihagyva: {ignoredIds.Count} | " +
            $"Exportálva: {cardsToExport.Count} | " +
            $"{OUTPUT_FILE}"
        );
    }


    // =========================================================
    // ID-K FELDOLGOZÁSA
    // =========================================================

    private HashSet<ushort> ParseIgnoredIds()
    {
        HashSet<ushort> result = new();


        if (string.IsNullOrWhiteSpace(
            ignoredIdsText
        ))
        {
            return result;
        }


        string[] parts =
            ignoredIdsText.Split(
                new[] { ',', ';', ' ', '\n' },
                System.StringSplitOptions
                    .RemoveEmptyEntries
            );


        foreach (string part in parts)
        {
            if (
                ushort.TryParse(
                    part.Trim(),
                    out ushort id
                )
            )
            {
                result.Add(id);
            }
            else
            {
                Debug.LogWarning(
                    $"Nem érvényes Card ID: {part}"
                );
            }
        }


        return result;
    }


    // =========================================================
    // ABLAK ALJA
    // =========================================================

    private void DrawCardInfo()
    {
        if (foundCards.Count == 0)
            return;


        HashSet<ushort> ignored =
            ParseIgnoredIds();


        int remaining =
            foundCards.Count(
                c => !ignored.Contains(c.cardId)
            );


        EditorGUILayout.LabelField(
            $"Exportálva lenne: {remaining}"
        );

        EditorGUILayout.LabelField(
            $"Kihagyva lenne: " +
            $"{foundCards.Count - remaining}"
        );
    }
}