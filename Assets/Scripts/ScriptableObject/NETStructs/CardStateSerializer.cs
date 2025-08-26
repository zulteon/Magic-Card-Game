using FishNet.Serializing;
using FishNet.CodeGenerating;
using System;
using System.Collections.Generic;

// Tiszta adatstruktúra
[System.Serializable]
public struct CardState : IEquatable<CardState>
{
    public ushort cardId;     // Globális kártya definíció ID
    public ushort sequenceId; // Meccsen belüli egyedi példány ID
    public int currentCost;   // Aktuális költség
    public List<ushort> activeEffects; // ÚJ: aktív effektek listája

    [NonSerialized] // hálózaton nem megy
    public bool canSeeCard;

    public bool Equals(CardState other)
    {
        return cardId == other.cardId &&
               sequenceId == other.sequenceId &&
               currentCost == other.currentCost;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(cardId, sequenceId, currentCost);
    }
}

// Hálózati serializer
[UseGlobalCustomSerializer]
public static class CardStateSerializer
{
    private const byte VERSION = 1; // változatlan

    [Flags]
    private enum Fields : ushort
    {
        None = 0,
        CardId = 1 << 0,
        SequenceId = 1 << 1,
        CurrentCost = 1 << 2,
        ActiveEffects = 1 << 3 // ÚJ mezõ
    }

    public static void WriteCardState(Writer writer, CardState value)
    {
        // 1) Verzió
        writer.WriteByte(VERSION);

        // 2) Flags – most mindig minden alap mezõt küldünk
        Fields flags = Fields.CardId | Fields.SequenceId | Fields.CurrentCost;

        if (value.activeEffects != null && value.activeEffects.Count > 0)
            flags |= Fields.ActiveEffects;

        writer.WriteUInt16((ushort)flags);

        // 3) Mezõk írása flags alapján
        if ((flags & Fields.CardId) != 0) writer.WriteUInt16(value.cardId);
        if ((flags & Fields.SequenceId) != 0) writer.WriteUInt16(value.sequenceId);
        if ((flags & Fields.CurrentCost) != 0) writer.WriteInt32(value.currentCost);

        if ((flags & Fields.ActiveEffects) != 0)
        {
            writer.WriteInt32(value.activeEffects.Count);
            for (int i = 0; i < value.activeEffects.Count; i++)
                writer.WriteUInt16(value.activeEffects[i]);
        }
    }

    public static CardState ReadCardState(Reader reader)
    {
        byte version = reader.ReadByte();

        if (version >= 1)
        {
            Fields flags = (Fields)reader.ReadUInt16();
            var cs = new CardState
            {
                activeEffects = new List<ushort>()
            };

            if ((flags & Fields.CardId) != 0) cs.cardId = reader.ReadUInt16();
            if ((flags & Fields.SequenceId) != 0) cs.sequenceId = reader.ReadUInt16();
            if ((flags & Fields.CurrentCost) != 0) cs.currentCost = reader.ReadInt32();

            if ((flags & Fields.ActiveEffects) != 0)
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    cs.activeEffects.Add(reader.ReadUInt16());
            }

            return cs;
        }

        throw new Exception($"CardState unknown serialization version: {version}");
    }
}
