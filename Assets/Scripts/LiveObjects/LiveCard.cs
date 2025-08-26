using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
public class LiveCard : NetworkBehaviour
{
    public Card c;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
     public int health;
     public int attack;
    public PlayerController playerController { get; private set; }
    
    public LiveCard Init(Card c) { 
        this.c = c;
       isSpell= c.IsSpell();
        cost = c.Cost;
        if (!isSpell) {
            health=c.m.health;
            attack=c.m.attack;
        }
        return this;
    }
    
    
    public int cost;
     public bool isSpell;
}
