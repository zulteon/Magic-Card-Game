using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
public static class MinionStateFactory
{
    public static MinionState FromCardState(CardState card, ushort sequenceId)
    {
        MinionCard c=(MinionCard)CardManager.instance.GetMinion(0);MinionState m=
        new MinionState
        {// if special buffs on card than change it to card.currenthealth or pass extra buff
            cardId = c.cardId,
            sequenceId = sequenceId,
            currentHealth = (ushort)c.health,
            attack = (short)c.attack,
            canAttack = true, // Summoning sickness
            activeEffects = new List<ushort>()
        };
        UnityEngine.Debug.Log(m.cardId.ToString()+"minion ka jött létre a gyárban" +m.attack+":"+m.currentHealth);
        return m;
    }
}