using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    public readonly Dictionary<ushort, Card> CardIdDictionary = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadAllCards();
    }

    private void LoadAllCards()
    {
        Card[] allCards = Resources.LoadAll<Card>("Cards");

        foreach (Card card in allCards)
        {
            if (CardIdDictionary.ContainsKey(card.cardId))
            {
                Debug.LogWarning($"Duplicate card ID found: {card.cardId} for card {card.name}. Skipping.");
                continue;
            }
            CardIdDictionary.Add(card.cardId, card);
        }

        Debug.Log($"Loaded {CardIdDictionary.Count} cards into the CardManager.");
    }

    public Card GetCard(ushort cardId)
    {
        if (CardIdDictionary.TryGetValue(cardId, out Card card))
        {
            return card;
        }
        Debug.LogError($"Card data not found for ID: {cardId}");
        return null;
    }
}