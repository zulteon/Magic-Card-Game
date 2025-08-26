using UnityEngine;

public class LiveEffect
{
    public Effect baseEffect;
    public int remainingTurns;

    public LiveEffect(Effect effect)
    {
        baseEffect = effect;
       //remainingTurns = effect.defaultDuration;
    }

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
