using UnityEngine;
using System.Collections.Generic;

public class LiveMinion : MonoBehaviour
{
    public ushort cardId;
    public ushort sequenceId;
    public ushort currentHealth;
    public short attack;
    public List<ushort> activeEffects = new List<ushort>();

    public void InitFromCardState(CardState card)
    {
        cardId = card.cardId;
        sequenceId = card.sequenceId;

        //var def = CardDatabase.Get(card.cardId);
       // attack = (short)def.BaseAttack;
        //currentHealth = (ushort)def.BaseHealth;
        //activeEffects.Clear();
    }

    public MinionState ToMinionState()
    {
        return new MinionState
        {
            cardId = cardId,
            sequenceId = sequenceId,
            currentHealth = currentHealth,
            attack = attack,
            activeEffects = new List<ushort>(activeEffects)
        };
    }
}
