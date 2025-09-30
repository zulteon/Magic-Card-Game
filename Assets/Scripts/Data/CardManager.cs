using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class CardManager : MonoBehaviour
{
    [Header("Runtime Data")]
    [SerializeReference]
    public List<CardData> allCards = new List<CardData>();

    public Dictionary<ushort, CardData> cardLookup = new Dictionary<ushort, CardData>();
    public static CardManager instance;

    void Start()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(this);

        // Példa: automatikusan StreamingAssets/cards.json-ból tölti
        LoadCardsFromFile(Path.Combine(Application.streamingAssetsPath, "Deck_deck.json"));
    }

    /// <summary>
    /// Betöltés fájlból (pl. StreamingAssets)
    /// </summary>
    public void LoadCardsFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"JSON fájl nem található: {path}");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            LoadCardsFromString(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Hiba a JSON fájl olvasásakor: {e.Message}");
        }
    }

    /// <summary>
    /// Betöltés közvetlen JSON stringbõl
    /// </summary>
   public void LoadCardsFromString(string json)
{
    try
    {
        var wrapper = JsonConvert.DeserializeObject<CardCollectionWrapper>(json);

        allCards.Clear();
        foreach (var raw in wrapper.cards)
        {
            string cardJson = JsonConvert.SerializeObject(raw);
            CardData card = CardJsonConverter.DeserializeCard(cardJson);
            if (card != null)
            {
                allCards.Add(card);
            }
        }

        BuildLookupTables();
        Debug.Log($"Betöltve {allCards.Count} kártya JSON-ból!");
    }
    catch (Exception e)
    {
        Debug.LogError($"Hiba a JSON feldolgozásakor: {e.Message}");
    }
}


    void BuildLookupTables()
    {
        cardLookup.Clear();
        foreach (var card in allCards)
        {
            cardLookup[card.cardId] = card;
        }
    }

    public CardData GetCard(ushort cardId)
    {
        cardLookup.TryGetValue(cardId, out CardData card);
        return card;
    }
    public MinionCard GetMinion(ushort cardId)
    {
        cardLookup.TryGetValue(cardId, out CardData card);
        print("returning "+card.cardId );
        return (MinionCard)card;
    }
}
[System.Serializable]
public class CardCollectionWrapper
{
    public List<Dictionary<string, object>> cards;
}
