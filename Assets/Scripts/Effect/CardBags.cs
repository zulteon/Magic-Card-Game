using System;
using System.Collections.Generic;
[Flags]
public enum Zone {None=0, Board=1, Deck=2, Hand=4, Graveyard =8}

/// <summary>
/// A NEM pályán lévõ lapok EffectBag-jei, kártya-sequenceId szerint.
/// A pályán a MinionLogic tartja a sajátját, a gyûjtõben nincs képesség.
/// </summary>
public class CardBags
{
    // CardBags
    public IReadOnlyDictionary<ushort, EffectBag> All => _bags;
    private readonly Dictionary<ushort, EffectBag> _bags = new();

    public EffectBag Get(ushort cardSeqId)
        => _bags.TryGetValue(cardSeqId, out var bag) ? bag : null;

    public EffectBag Create(ushort cardSeqId)
    {
        var bag = new EffectBag(cardSeqId);
        _bags[cardSeqId] = bag;
        return bag;
    }

    public void Remove(ushort cardSeqId, RemoveReason reason)
    {
        if (!_bags.TryGetValue(cardSeqId, out var bag)) return;
        bag.DisposeAll(reason);
        _bags.Remove(cardSeqId);
    }
}
/*// GameManager
public readonly CardBags cardBags = new();

[Server]
public void MoveCard(ushort cardSeqId, Zone from, Zone to)
{
    // 1. A régi zóna képességei lebomlanak
    cardBags.Remove(cardSeqId, RemoveReason.ZoneChange);

    // 2. Az új zónáé felépül — bag csak Deck/Hand esetén kell
    if (to == Zone.Deck || to == Zone.Hand)
    {
        var bag = cardBags.Create(cardSeqId);
        RegisterCardAbilities(cardSeqId, bag, to);
    }
}*/