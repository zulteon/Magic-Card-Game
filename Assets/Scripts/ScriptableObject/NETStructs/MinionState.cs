using System;
using System.Collections.Generic;
using FishNet.Serializing;

[Serializable]
public struct MinionState : IEquatable<MinionState>
{
    public ushort cardId;         // Kártya definíció ID
    public ushort sequenceId;     // Meccsen belüli egyedi példány ID

    public ushort currentHealth;  // Aktuális HP
    public short attack;          // ATK (lehet negatív is)
    public bool canAttack;
    public bool taunt;
    public int maxHealth;
    // FIGYELEM: sose módosítsd a meglévõ listát (Add/Remove/Clear)!
    // A struct másolásakor a lista referenciája osztott, így a dirty-check
    // nem venné észre a változást. Mindig új listát adj:
    //     s.activeEffects = new List<ushort>(régi) { újId };
    public List<ushort> activeEffects; // Buff/Debuff ID-k

    public bool Equals(MinionState other)
    {
        if (cardId != other.cardId ||
        sequenceId != other.sequenceId ||
        currentHealth != other.currentHealth ||
        attack != other.attack || canAttack != other.canAttack || taunt!=other.taunt ||maxHealth!=other.maxHealth)
                return false;

            int a = activeEffects?.Count ?? 0;
            int b = other.activeEffects?.Count ?? 0;
            if (a != b) return false;

            for (int i = 0; i < a; i++)
                if (activeEffects[i] != other.activeEffects[i]) return false;

            return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            hash = hash * 31 + cardId.GetHashCode();
            hash = hash * 31 + sequenceId.GetHashCode();
            hash = hash * 31 + currentHealth.GetHashCode();
            hash = hash * 31 + attack.GetHashCode();
            hash = hash * 31 + canAttack.GetHashCode();
            hash = hash * 31 + taunt.GetHashCode();
            hash = hash * 31 + maxHealth.GetHashCode();
            
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
