using FishNet.Serializing;
using FishNet.CodeGenerating;
using System;
using System.Collections.Generic;

[UseGlobalCustomSerializer]
public static class MinionStateSerializer
{
    private const byte VERSION = 1;

    [Flags]
    private enum Fields : ushort
    {
        None = 0,
        CardId = 1 << 0,
        SequenceId = 1 << 1,
        CurrentHealth = 1 << 2,
        Attack = 1 << 3,
        ActiveEffects = 1 << 4,
        CanAttack = 1 << 5
    }

    public static void WriteMinionState(Writer writer, MinionState value)
    {
        writer.WriteByte(VERSION);

        bool hasEffects = value.activeEffects != null && value.activeEffects.Count > 0;

        Fields flags = Fields.CardId | Fields.SequenceId | Fields.CurrentHealth | Fields.Attack| Fields.CanAttack;
        if (hasEffects)
            flags |= Fields.ActiveEffects;

        writer.WriteUInt16((ushort)flags);

        if ((flags & Fields.CardId) != 0) writer.WriteUInt16(value.cardId);
        if ((flags & Fields.SequenceId) != 0) writer.WriteUInt16(value.sequenceId);
        if ((flags & Fields.CurrentHealth) != 0) writer.WriteUInt16(value.currentHealth);
        if ((flags & Fields.Attack) != 0) writer.WriteInt16(value.attack);
        if ((flags & Fields.CanAttack) != 0) writer.WriteBoolean(value.canAttack);
        if ((flags & Fields.ActiveEffects) != 0)
        {
            var list = value.activeEffects;
            if (list != null)  // ✅ Null check
            {
                writer.WriteInt32(list.Count);
                for (int i = 0; i < list.Count; i++)
                    writer.WriteUInt16(list[i]);
            }
            else
            {
                writer.WriteInt32(0);  // Üres lista jelzése
            }
        }
    }

    public static MinionState ReadMinionState(Reader reader)
    {
        byte version = reader.ReadByte();

        if (version >= 1)
        {
            Fields flags = (Fields)reader.ReadUInt16();

            var ms = new MinionState();

            if ((flags & Fields.CardId) != 0) ms.cardId = reader.ReadUInt16();
            if ((flags & Fields.SequenceId) != 0) ms.sequenceId = reader.ReadUInt16();
            if ((flags & Fields.CurrentHealth) != 0) ms.currentHealth = reader.ReadUInt16();
            if ((flags & Fields.Attack) != 0) ms.attack = reader.ReadInt16();
            if ((flags & Fields.CanAttack) != 0) ms.canAttack = reader.ReadBoolean();
            if ((flags & Fields.ActiveEffects) != 0)
            {
                int count = reader.ReadInt32();
                var list = new List<ushort>(count);
                for (int i = 0; i < count; i++)
                    list.Add(reader.ReadUInt16());
                ms.activeEffects = list;
            }
            else
            {
                ms.activeEffects = null;
            }

            return ms;
        }

        throw new Exception($"MinionState unknown serialization version: {version}");
    }
}
