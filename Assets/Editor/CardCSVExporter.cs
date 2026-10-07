using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CardCsvExporter
{
    private const string CardFolder = "Assets/Real_Cards";
    private const string OutputPath = "Assets/CardOverview.csv";

    [MenuItem("Tools/Cards/Export Card Overview CSV")]
    public static void Export()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Card",
            new[] { CardFolder }
        );

        var cards = guids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Select(path => AssetDatabase.LoadAssetAtPath<Card>(path))
            .Where(card => card != null)
            .OrderBy(card => card.cardId)
            .ToList();

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("CardId;CardName;Description;Cost;Attack;Health;Sprite");

        foreach (Card card in cards)
        {
            string cardName = card.name;
            string description = "";
            string cost = card.Cost.ToString();
            string attack = "";
            string health = "";
            string sprite = "";

            if (card.m != null)
            {
                description = card.m.description;
                attack = card.m.attack.ToString();
                health = card.m.health.ToString();
                sprite = card.m.sprite;
            }
            else if (card.spell != null)
            {
                description = card.spell.description;
                sprite = card.spell.sprite;
            }

            csv.AppendLine(
                $"{card.cardId};" +
                $"{Csv(cardName)};" +
                $"{Csv(description)};" +
                $"{cost};" +
                $"{attack};" +
                $"{health};" +
                $"{Csv(sprite)}"
            );
        }
        File.WriteAllText(OutputPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();

        Debug.Log($"Card CSV kész: {cards.Count} kártya");
    }

    private static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}