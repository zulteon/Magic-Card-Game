using System;
using System.Diagnostics;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class MinionLogic
{
    public ushort _sequenceId,cardId;
    bool sleep;
    bool frozen;
    public int maxhealth;
    public EffectBag effectBag;
    internal static GameManager manager=>GameManager.instance;
    private MinionState State
    {
        get => manager.GetMinionById(_sequenceId);
        set => manager.ChangeMinionById(_sequenceId,minion=> minion=value);
                                                            // ide elég lenne _=>value
    }
    
    public ushort Health
    {
        get => State.currentHealth;
        set
        {
            var s = State;
            s.currentHealth = value;
            State = s; // Frissíti a hálózatot
        }
    }
    public MinionLogic(ushort sequenceId)
    {
        _sequenceId = sequenceId;
        ushort cardId= manager.GetMinionById(sequenceId).cardId;
        effectBag = new EffectBag(_sequenceId);
       // MinionCard cardData = CardManager.instance.GetMinion(cardId);
       //  maxhealth=cardData.health;
    }
    public void Attack(int  damage,ushort victimId,bool forced=false)
    {                   //damage fölösleges
        var state = State;
        MinionState victim = manager.GetMinionById(victimId);
        victim.currentHealth = (ushort)UnityEngine.Mathf.Max(0, victim.currentHealth - state.attack);
        state.currentHealth = (ushort)UnityEngine.Mathf.Max(0, state.currentHealth - victim.attack);
        // ide jön a CLient Event küldés
        ClientEvent attackEv = new ClientEvent
        {
            effectType = (ushort)Effect.Type.attack,
            doerId = _sequenceId,
            targetIds = new ushort[] { victimId }, // Sorrend: 0: Attacker, 1: Victim
            value = state.attack, // Animációhoz: mekkora ütés látszódjon
            newValues = new int[] { state.currentHealth, victim.currentHealth } // Snapshot a jövőbeli HP-król
        };
        EffectClient.instance.AddEvent(attackEv);
        State = state;
        manager.ChangeMinionById(victimId, minion => minion = victim);
    }
    public void CopyStats(Vector2Int copyFlags, short sourceAttack, ushort sourceHealth)
    {
        var state = State;

        if (copyFlags.x != 0)
            state.attack = sourceAttack;

        if (copyFlags.y != 0)
            state.currentHealth = sourceHealth;

        EffectClient.instance.AddEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.copyStats,
            doerId = _sequenceId,
            targetIds = new ushort[] { _sequenceId },
            value = 0,
            newValues = new int[] { state.attack, state.currentHealth }
        });

        State = state;
    }
    // MinionLogic.cs - Damage függvény
    public void Damage(int damage, ushort attackerId = 0)
    {
        UnityEngine.Debug.Log("damaging " +damage.ToString());
        var state = State;
        state.currentHealth = (ushort)UnityEngine.Mathf.Max(0, state.currentHealth - damage);

        // ✨ DIREKT ITT küldjük el, MIUTÁN a HP megváltozott
        ClientEvent ev = new ClientEvent
        {
            effectType = (ushort)Effect.Type.damage,
            targetIds = new ushort[] { _sequenceId },
            value = damage,
            newValues = new int[] { state.currentHealth }, // ← JÓ érték!
            doerId = attackerId
        };
        EffectClient.instance.AddEvent(ev);

        State = state;
        if(state.currentHealth < 1)
            Death();
    }
    public void Buff(int attackBonus, int healthBonus)
    {
        var state = State;
        UnityEngine.Debug.Log(" BUffoljad " + healthBonus);
        state.attack = (short)Math.Clamp(
            state.attack + attackBonus,
            short.MinValue,
            short.MaxValue
        );

        state.currentHealth = (ushort)Math.Clamp(
            state.currentHealth + healthBonus,
            0,
            ushort.MaxValue
        );

        EffectClient.instance.AddEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.buff,
            targetIds = new ushort[] { _sequenceId },
            value = attackBonus,
            newValues = new int[]
            {
            state.attack,
            state.currentHealth
        },
            doerId = _sequenceId
        });

        State = state;
    }
    public void Sleep(bool sleep = true)
    {
        this.sleep = sleep;
    }
    public void Freeze(bool freeze = true)
    {
        this.frozen = freeze;
    }
    private void Die()
    {
        //send to gamemanager. but open graveyard in game manager if multiple actions ... 
    }

    // MinionLogic.cs
    // MinionLogic.cs
    internal void Heal(int value, ushort healerId = 0)
    {
        var state = State;

        // Max HP lekérése
        MinionCard cardData = CardManager.instance.GetMinion(state.cardId);
        ushort maxHealth = (ushort)cardData.health;

        // Heal alkalmazása
        state.currentHealth = (ushort)UnityEngine.Mathf.Min(maxHealth, state.currentHealth + value);

        // ✨ Esemény küldése a kliensnek
        ClientEvent ev = new ClientEvent
        {
            effectType = (ushort)Effect.Type.heal,
            targetIds = new ushort[] { _sequenceId },
            value = value,
            newValues = new int[] { state.currentHealth },
            doerId = healerId
        };
        EffectClient.instance.AddEvent(ev);

        UnityEngine.Debug.Log($"Healed {value}, new health: {state.currentHealth}");
        State = state;
    }
    public void Steal(Vector2Int amount, ushort thiefId)
    {
        if (!manager.HasMinion(thiefId))
        {
            UnityEngine.Debug.LogWarning($"Steal: thief {thiefId} not found");
            return;
        }

        var victimState = State; // ez a minion van megcsapolva — ezen fut a metódus
        var thief = manager.GetMinionById(thiefId);

        // clamp: nem lophat többet, mint amennyi ténylegesen van
        int actualAttackSteal = UnityEngine.Mathf.Min(amount.x, victimState.attack);
        int actualHealthSteal = UnityEngine.Mathf.Min(amount.y, victimState.currentHealth);

        victimState.attack = (short)Math.Clamp(victimState.attack - actualAttackSteal, short.MinValue, short.MaxValue);
        victimState.currentHealth = (ushort)Math.Clamp(victimState.currentHealth - actualHealthSteal, 0, ushort.MaxValue);

        thief.attack = (short)Math.Clamp(thief.attack + actualAttackSteal, short.MinValue, short.MaxValue);
        thief.currentHealth = (ushort)Math.Clamp(thief.currentHealth + actualHealthSteal, 0, ushort.MaxValue);

        EffectClient.instance.AddEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.steal,
            doerId = thiefId,
            targetIds = new ushort[] { _sequenceId }, // a victim maga
            value = actualAttackSteal,
            newValues = new int[] { thief.attack, thief.currentHealth, victimState.attack, victimState.currentHealth }
        });

        State = victimState; // this = victim, frissítjük
        manager.ChangeMinionById(thiefId, m => m = thief);
    }
    public void SwapAttackHealth()
    {
        var state = State;

        short oldAttack = state.attack;
        ushort oldHealth = state.currentHealth;

        // csere: az Attack lesz az új Health, a Health lesz az új Attack
        state.attack = (short)Math.Clamp(oldHealth, short.MinValue, short.MaxValue);
        state.currentHealth = (ushort)Math.Clamp(oldAttack, 0, ushort.MaxValue);

        EffectClient.instance.AddEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.swapAttackHealth,
            doerId = _sequenceId,
            targetIds = new ushort[] { _sequenceId },
            value = 0, // nincs "mennyiség", a csere maga az esemény
            newValues = new int[] { state.attack, state.currentHealth }
        });

        State = state;
    }
    public void SetStats(int attack, int health)
    {
        var state = State;

        // Sentinel-logika: negatív érték = "ne módosítsd ezt a mezőt"
        if (attack >= 0)
            state.attack = (short)attack;
        if (health > 0)
            state.currentHealth = (ushort)health;

        EffectClient.instance.AddEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.setStats,
            doerId = _sequenceId,
            targetIds = new ushort[] { _sequenceId },
            value = 0,
            newValues = new int[] { state.attack, state.currentHealth }
        });

        State = state;
    }
    public void GainEconomy(ushort id, int amount)
    {
        var player = manager.GetPlayer(!manager.isAllyMinion(id));
        player.economy.RaiseResource(amount);

        EffectClient.instance.AddEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.gainEconomy,
            doerId = _sequenceId,
            targetIds = new ushort[] { id },
            value = amount,
            newValues = new int[] { player.economy.CurrentResource }
        });
    }
    public enum RemoveReason { Death, Silence, ReturnToHand }

    // MinionLogic
    private void Death()
    {
        GameManager.instance.graveyard.SendToGraveyard(_sequenceId);
    }

}

public class LiveGuard
{
    // OnWhen Guard , Effect Type is the one who block
    public Effect.Type blocks;
    public int charges = 1;       // hány alkalommal fog el; -1 = korlátlan
    public int every = 1;
    public int seen;
    public MinionLogic owner;
    public ushort sourceId;
    public int expiresOnTurn = -1;
    public Effect originalEffect;   // később: effectId (szerializálhatóság)

    public bool IsSpent => charges == 0;

    public bool TryConsume()
    {
        seen++;
        if (every > 1 && seen % every != 0) return false;
        if (charges > 0) charges--;
        return true;
    }
}
