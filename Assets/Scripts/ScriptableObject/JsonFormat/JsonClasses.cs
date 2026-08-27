using System;
using System.Collections.Generic;
using UnityEngine;

// Enum a kártyatípusokhoz
public enum CardType
{
    Minion,
    Spell
}

// Alapkártya absztrakt osztály
[System.Serializable]
public abstract class CardData
{
    public ushort cardId;
    public int cost;
    public string description;
    public string cardType; // JSON deserializáláshoz kell
    public string sprite;

    public List<ushort> effectIds = new List<ushort>();
    public abstract CardType GetCardType();
    public Dictionary<string, int> dynamicValues = new Dictionary<string, int>();

}

// Minion kártya
[System.Serializable]
public class MinionCard : CardData
{
    public int attack;
    public int health;
    
    public string effect;
    public int raceId;
    public bool charge;
    public MinionCard()
    {
        cardType = "minion";
    }
    
    public override CardType GetCardType() => CardType.Minion;
}

// Spell kártya
[System.Serializable]
public class SpellCard : CardData
{
    public string effect;

    public SpellCard()
    {
        cardType = "spell";
    }

    public override CardType GetCardType() => CardType.Spell;
}

// Kártya gyûjtemény
[System.Serializable]
public class CardCollection
{
    public List<CardData> cards = new List<CardData>();
}

// JSON Converter a polimorf deserializáláshoz
public static class CardJsonConverter
{
    public static CardData DeserializeCard(string json)
    {
        var tempCard = JsonUtility.FromJson<TempCardData>(json);
        switch (tempCard.cardType.ToLower())
        {
            case "minion":
                return JsonUtility.FromJson<MinionCard>(json);
            case "spell":
                return JsonUtility.FromJson<SpellCard>(json);
            default:
                Debug.LogError($"Ismeretlen kártya típus: {tempCard.cardType}");
                return null;
        }
    }

    [System.Serializable]
    private class TempCardData
    {
        public string cardType;
    }
}

// JSON wrapper
[System.Serializable]
public class JsonCardWrapper
{
    public List<System.Object> cards;
}