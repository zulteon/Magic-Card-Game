using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.Xml;

[CreateAssetMenu(fileName = "Deck", menuName = "Card Creator/deck")]
public class Deck : ScriptableObject
{
    public List<Card> deck;

    // Deck konvert�l�sa JSON-kompatibilis form�tumra
    public CardCollection ToJson()
    {
        CardCollection cardCollection = new CardCollection();

        Debug.Log($"Deck neve: {name}, k�rty�k sz�ma: {deck?.Count ?? 0}");

        if (deck == null)
        {
            Debug.LogWarning("A deck lista null!");
            return cardCollection;
        }

        foreach (Card card in deck)
        {
            if (card == null)
            {
                Debug.LogWarning("Null k�rtya tal�lva a deck-ben!");
                continue;
            }

            Debug.Log($"Feldolgoz�s: {card.name} (ID: {card.cardId})");

            CardData cardData = null;

            // Minion k�rtya konvert�l�sa
            if (card.m != null)
            {
                Debug.Log($"Minion k�rtya: {card.m.attack}/{card.m.health}");
                cardData = new MinionCard
                {
                    cardId = card.cardId,
                    cost = card.Cost,
                    description = card.description,
                    attack = card.m.attack,
                    health = card.m.health,
                    sprite = card.m.sprite,
                    effect = card.m.effect,
                    effectIds = ConvertEffectsToIds(card.m.e),
                    raceId = (int)card.m.race
                };
            }
            // Spell k�rtya konvert�l�sa
            else if (card.spell != null)
            {
                Debug.Log($"Spell k�rtya: {card.spell.description}");
                cardData = new SpellCard
                {
                    cardId = card.cardId,
                    cost = card.Cost,
                    description = card.description,
                    sprite = card.spell.sprite,
                    effect = card.spell.effect,
                    effectIds = ConvertEffectsToIds(card.spell.e)
                };
            }
            else
            {
                Debug.LogWarning($"K�rtya sem minion, sem spell: {card.name}");
            }

            if (cardData != null)
            {
                cardCollection.cards.Add(cardData);
                Debug.Log($"K�rtya hozz�adva: {cardData.cardId}");
            }
        }

        Debug.Log($"V�gs� k�rty�k sz�ma: {cardCollection.cards.Count}");
        return cardCollection;
    }

    // JSON string-k�nt val� export�l�s
    public string ToJsonString()
    {
        CardCollection collection = ToJson();
        // sz�pen form�zott JSON, t�pusinf�kkal
        return JsonConvert.SerializeObject(collection, Newtonsoft.Json.Formatting.Indented,
     new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
    }
    // Effect-ek ID-kk� konvert�l�sa
    private List<int> ConvertEffectsToIds(List<Effect> effects)
    {
        List<int> ids = new List<int>();
        if (effects != null)
        {
            foreach (Effect effect in effects)
            {
                if (effect != null)
                {
                    ids.Add(effect.GetInstanceID()); // vagy effect.id ha van
                }
            }
        }
        return ids;
    }

    [ContextMenu("Export Deck to JSON")]
    public void ExportDeckToJson()
    {
        string json = ToJsonString();
        string path = Application.dataPath + $"/StreamingAssets/{name}_deck.json";

        // StreamingAssets mappa l�trehoz�sa ha nem l�tezik
        if (!System.IO.Directory.Exists(Application.dataPath + "/StreamingAssets"))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/StreamingAssets");
        }

        System.IO.File.WriteAllText(path, json);
        Debug.Log($"Deck export�lva: {path}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

[UnityEditor.MenuItem("Assets/Export Deck to JSON", false, 20)]
static void ExportSelectedDeck()
{
    Deck selectedDeck = UnityEditor.Selection.activeObject as Deck;
    if (selectedDeck != null)
    {
        selectedDeck.ExportDeckToJson();
    }
    else
    {
        Debug.LogWarning("V�lassz ki egy Deck ScriptableObject-et!");
    }
}
}