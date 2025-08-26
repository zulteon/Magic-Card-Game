using System;
using System.Collections.Generic;
using FishNet.Serializing;

[Serializable]
public struct MinionState : IEquatable<MinionState>
{
    public ushort cardId;         // Kártya definíció ID
    public ushort sequenceId;     // Meccsen belüli egyedi példány ID
    public ushort ownerNetId;     // Tulajdonos játékos NetId

    public ushort currentHealth;  // Aktuális HP
    public short attack;          // ATK (lehet negatív is)

    public List<ushort> activeEffects; // Buff/Debuff ID-k

    public bool Equals(MinionState other)
    {
        if (cardId != other.cardId ||
            sequenceId != other.sequenceId ||
            ownerNetId != other.ownerNetId ||
            currentHealth != other.currentHealth ||
            attack != other.attack)
            return false;

        if (activeEffects == null && other.activeEffects == null)
            return true;
        if (activeEffects == null || other.activeEffects == null)
            return false;
        if (activeEffects.Count != other.activeEffects.Count)
            return false;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i] != other.activeEffects[i])
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            hash = hash * 31 + cardId.GetHashCode();
            hash = hash * 31 + sequenceId.GetHashCode();
            hash = hash * 31 + ownerNetId.GetHashCode();
            hash = hash * 31 + currentHealth.GetHashCode();
            hash = hash * 31 + attack.GetHashCode();

            if (activeEffects != null)
            {
                for (int i = 0; i < activeEffects.Count; i++)
                {
                    hash = hash * 31 + activeEffects[i].GetHashCode();
                }
            }

            return hash;
        }
    }
}
