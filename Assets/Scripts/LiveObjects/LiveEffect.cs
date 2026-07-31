using UnityEngine;

public class LiveEffect_old
{
    public Effect baseEffect;
    public int remainingTurns;

  

    public bool TickTurn()
    {
        if (remainingTurns > 0)
        {
            remainingTurns--;
            return remainingTurns == 0; // True, ha lejárt
        }
        return false;
    }
}
