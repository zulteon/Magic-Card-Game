using UnityEngine;

public class GameState
{
    public PlayerState[] players = new PlayerState[2]; // [0] és [1]
    public int turn;
    public int currentPlayerIndex;

    public PlayerState CurrentPlayer => players[currentPlayerIndex];
    public PlayerState OpponentPlayer => players[1 - currentPlayerIndex];
    public GameState()
    {
        players[0] = new PlayerState();
        players[1] = new PlayerState();
    }
}