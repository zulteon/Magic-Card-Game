using System.Collections.Generic;
public static class MinionStateFactory
{
    public static MinionState FromCardState(CardState card, ushort sequenceId)
    {
        
        MinionCard c = (MinionCard)CardManager.instance.GetMinion(card.cardId);bool taunt=false;
        ushort health = (ushort)(c.health + card.healthBonus);
        foreach (var i in c.effectIds)
            if (i == 11)
                taunt = true;
                var m = new MinionState
        {
            cardId = c.cardId,
            sequenceId = sequenceId,
            canAttack = c.charge,
            currentHealth = health,
             maxHealth = health,
            attack = (short)(c.attack + card.attackBonus),
            taunt = taunt,
            activeEffects = new List<ushort>(c.effectIds) // valszeg fölösleges , figyeljünk az effect lekéréséket a triggercheckbe ha kiszedjük
        };

        UnityEngine.Debug.Log($"{m.cardId} minionka jött létre a gyárban : {m.attack}/{m.currentHealth} (bázis {c.attack}/{c.health} + bonus {card.attackBonus}/{card.healthBonus})");
        return m;
    }
    public static MinionState FromMinionData(ushort cardId,ushort sequenceId)
    {
        MinionCard c = (MinionCard)CardManager.instance.GetMinion(cardId);
        var m = new MinionState
        {
            cardId = c.cardId,
            sequenceId = sequenceId,
            canAttack = c.charge,
            currentHealth = (ushort)(c.health ),
            maxHealth = (ushort)(c.health ),
            attack = (short)(c.attack),
            activeEffects = new List<ushort>(c.effectIds)
        };

        UnityEngine.Debug.Log($"{m.cardId} apróbb minionka jött létre a gyárban : {m.attack}/{m.currentHealth}");
        return m;
    }
}