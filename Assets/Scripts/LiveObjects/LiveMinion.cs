using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;


/*
┌─────────────────────────────────────────────────────────────┐
│  SCRIPTABLE OBJECT (Template)                                │
├─────────────────────────────────────────────────────────────┤
│  MinionData.cs  - Unity asset, innen jönnek a base statok   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  SERVER OLDAL (FishNet)                                      │
├─────────────────────────────────────────────────────────────┤
│  MinionLogic.cs    - Szerver logika (HP, damage, stb)       │
│  MinionState.cs    - [SyncVar] struct (hálózaton megy)      │
│  MinionSerializer  - FishNet szerializáció                   │
│  LiveHero.cs       - MinionLogic + hero specifikus           │
└─────────────────────────────────────────────────────────────┘
                            ↓ (SyncVar)
┌─────────────────────────────────────────────────────────────┐
│  CLIENT OLDAL (UI/Vizualizáció)                              │
├─────────────────────────────────────────────────────────────┤
│  MinionView.cs     - UI megjelenítés (HP bar, animáció)     │
│  LiveMinion.cs     - Kliens oldali példány (input küldés)   │
└─────────────────────────────────────────────────────────────┘
 /*/
public class LiveMinion : MonoBehaviour
{
    public ushort cardId;
    public  ushort sequenceId;
    public ushort currentHealth;
    public short attack;
    public bool ally=true;// need to implement
    public List<ushort> activeEffects = new List<ushort>();
    public bool validTarget { get; set; }
    public void InitFromMinionState(MinionState minion)
    {
        cardId = minion.cardId;
        sequenceId = minion.sequenceId;
        attack = minion.attack;
        currentHealth = minion.currentHealth;
        if (minion.taunt)
            GetComponent<MinionView>().TauntUI();
        //var def = CardDatabase.Get(card.cardId);  
       // attack = (short)def.BaseAttack;
        //currentHealth = (ushort)def.BaseHealth;
        //activeEffects.Clear();
    }
    private void OnMouseDown()
    {// client only
        

    }

    public void StartAttackClick()
    {
        if (!ally || !CanAttack()) { print("nem tudunk támadni"); return; }

         GameManager.instance.GetLocalPlayerController().StartAttack(this);
    }
    public MinionState ToMinionState()
    {
        return new MinionState
        {
            cardId = cardId,
            sequenceId = sequenceId,
            currentHealth = currentHealth,
            attack = attack,
            canAttack =true,
            activeEffects = new List<ushort>(activeEffects)
        };
    }
    public void GetMinionState()
    {

    }
    [Client]
    public bool CanAttack()
    {
        if (!GameManager.instance.GetMinionById(sequenceId).canAttack) return false;
        return true;
    }
    public void AttackDamageApply(int amount)
    {
        currentHealth -=(ushort) amount;
        GetComponent<MinionView>().PlayDamageAnimation(amount);
       GetComponent<MinionView>().UpdateHealthVisual(currentHealth);
    }
}
