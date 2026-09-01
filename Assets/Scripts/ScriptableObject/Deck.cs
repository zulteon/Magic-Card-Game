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

    // Deck konvertálása JSON-kompatibilis formátumra
    public CardCollection ToJson()
    {
        return CardsToCollection(deck);
    }
    public static CardCollection CardsToCollection(IEnumerable<Card> cards)
    {
        CardCollection cardCollection = new CardCollection();

        foreach (Card card in cards)
        {
            if (card == null)
                continue;

            CardData cardData = null;

            if (card.m != null)
            {
                cardData = new MinionCard
                {
                    cardId = card.cardId,
                    cardName = card.name,
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
            else if (card.spell != null)
            {
                cardData = new SpellCard
                {
                    cardId = card.cardId,
                    cardName = card.name,
                    cost = card.Cost,
                    description = card.description,

                    sprite = card.spell.sprite,

                    effectIds = ConvertEffectsToIds(card.spell.e)
                };
            }

            if (cardData != null)
                cardCollection.cards.Add(cardData);
        }

        return cardCollection;
    }

    // JSON string-ként való exportálás
    public string ToJsonString()
    {
        CardCollection collection = ToJson();
        // szépen formázott JSON, típusinfókkal
        return JsonConvert.SerializeObject(collection, Newtonsoft.Json.Formatting.Indented,
     new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
    }
    // Effect-ek ID-kká konvertálása
    private static List<ushort> ConvertEffectsToIds(List<Effect> effects)
    {
        List<ushort> ids = new List<ushort>();
        if (effects != null)
        {
            foreach (Effect effect in effects)
            {
                if (effect != null)
                {
                    ids.Add(effect.effectId); 
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

        // StreamingAssets mappa létrehozása ha nem létezik
        if (!System.IO.Directory.Exists(Application.dataPath + "/StreamingAssets"))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/StreamingAssets");
        }

        System.IO.File.WriteAllText(path, json);
        Debug.Log($"Deck exportálva: {path}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
#if UNITY_EDITOR
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
        Debug.LogWarning("Válassz ki egy Deck ScriptableObject-et!");
    }
}
#endif
}