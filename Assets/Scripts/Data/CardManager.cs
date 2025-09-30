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

        // P�lda: automatikusan StreamingAssets/cards.json-b�l t�lti
        LoadCardsFromFile(Path.Combine(Application.streamingAssetsPath, "Deck_deck.json"));
    }

    /// <summary>
    /// Bet�lt�s f�jlb�l (pl. StreamingAssets)
    /// </summary>
    public void LoadCardsFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"JSON f�jl nem tal�lhat�: {path}");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            LoadCardsFromString(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Hiba a JSON f�jl olvas�sakor: {e.Message}");
        }
    }

    /// <summary>
    /// Bet�lt�s k�zvetlen JSON stringb�l
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
        Debug.Log($"Bet�ltve {allCards.Count} k�rtya JSON-b�l!");
    }
    catch (Exception e)
    {
        Debug.LogError($"Hiba a JSON feldolgoz�sakor: {e.Message}");
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
