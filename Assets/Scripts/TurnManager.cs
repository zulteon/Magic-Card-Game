using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{// this is probably a server only script.

    int turn = 1;
    public void EndTurn() {   //maybe IENumerator??
                                     // check out end of turn effect
        StartTurn();
    }

    private void StartTurn()
    {
        turn++;
        // Checkout startof turn effect
    }
}
