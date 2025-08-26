using UnityEngine;
using System.Collections.Generic;
public class LiveDeck 
{
    private List<LiveCard> deck;

    public void Init(Deck d)
    {
        deck = new List<LiveCard>();
        foreach (var card in d.deck)
        {
            deck.Add(new LiveCard().Init(card)); // Másolatot készítünk
        }

        Shuffle();
    }

    public void Shuffle()
    {
        // Fisher-Yates algoritmus (in-place shuffle)
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    public LiveCard Draw()
    {
        if (deck.Count == 0)
        {
            Debug.LogWarning("Nincs több lap a pakliban!");
            return null;
        }

        var top = deck[0];
        deck.RemoveAt(0);
        return top;
    }
}
