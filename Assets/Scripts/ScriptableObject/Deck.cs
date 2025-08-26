using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Deck", menuName = "Card Creator/deck")]
public class Deck :ScriptableObject
{
    public List<Card> deck;
}
