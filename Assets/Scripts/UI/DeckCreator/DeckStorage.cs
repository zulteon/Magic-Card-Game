using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Pakli mentés/betöltés a kártyakészítõhöz. Ugyanazt a JSON-formátumot
/// használja, mint a Deck.ExportDeckToJson — nincs külön adatosztály.
/// </summary>
public static class DeckStorage
{
    private static string Folder =>
        Path.Combine(Application.persistentDataPath, "Decks");

    public static void Save(string deckName, List<CardData> cards)
    {
        Directory.CreateDirectory(Folder);

        var collection = new CardCollection();
        collection.cards.AddRange(cards);

        string json = JsonConvert.SerializeObject(collection, Formatting.Indented);
        File.WriteAllText(Path.Combine(Folder, deckName + ".json"), json);

        Debug.Log($"Pakli mentve: {deckName} ({cards.Count} lap)");
    }

    public static List<CardData> Load(string deckName)
    {
        string path = Path.Combine(Folder, deckName + ".json");
        var result = new List<CardData>();

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Nincs ilyen pakli: {deckName}");
            return result;
        }

        var wrapper = JsonConvert.DeserializeObject<CardCollectionWrapper>(File.ReadAllText(path));

        foreach (var raw in wrapper.cards)
        {
            var card = CardJsonConverter.DeserializeCard(JsonConvert.SerializeObject(raw));
            if (card != null) result.Add(card);
        }

        return result;
    }

    public static List<string> ListDecks()
    {
        if (!Directory.Exists(Folder)) return new List<string>();

        var names = new List<string>();
        foreach (var f in Directory.GetFiles(Folder, "*.json"))
            names.Add(Path.GetFileNameWithoutExtension(f));

        return names;
    }
}