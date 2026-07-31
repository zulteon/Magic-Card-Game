using System.Collections.Generic;
public static class MinionStateFactory
{
    public static MinionState FromCardState(CardState card, ushort sequenceId)
    {
        MinionCard c = (MinionCard)CardManager.instance.GetMinion(card.cardId);

        var m = new MinionState
        {
            cardId = c.cardId,
            sequenceId = sequenceId,
            currentHealth = (ushort)(c.health + card.healthBonus),
            attack = (short)(c.attack + card.attackBonus),
            activeEffects = new List<ushort>()
        };

        UnityEngine.Debug.Log($"{m.cardId} minionka jött létre a gyárban : {m.attack}/{m.currentHealth} (bázis {c.attack}/{c.health} + bonus {card.attackBonus}/{card.healthBonus})");
        return m;
    }
}