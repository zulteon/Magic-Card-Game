
using UnityEngine;

public class PlayerState
{
    public Deck deck ;
    public Hero hero;
    public bool isLocalPlayer; // csak a UI tudja értelmezni
    public string playerId; // késõbb multiplayernél fontos lehet
}

