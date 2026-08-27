using System.Diagnostics;

public class PlayerEconomy
{
    private PlayerController _owner;
    int extraResourceNextTurn;
    public PlayerEconomy(PlayerController owner)
    {
        _owner = owner;
    }

    

    public bool TrySpendResource(int amount)
    {
        if (_owner.currentResource.Value < amount) return false;
        _owner.currentResource.Value -= amount;
        return true;
    }

    private const int MaxCrystals = 10;

    public void StartTurn()
    {
        if (_owner.maxResource.Value < MaxCrystals)
            _owner.maxResource.Value++;
        
        _owner.currentResource.Value = _owner.maxResource.Value+extraResourceNextTurn;
        
        UnityEngine.Debug.Log("We gained "+_owner.maxResource.Value+"+"+extraResourceNextTurn);
        extraResourceNextTurn = 0;
        if (_owner.currentResource.Value < 0)
        {
            extraResourceNextTurn = _owner.currentResource.Value;
            _owner.currentResource.Value = 0;
        }
    }

    public void RaiseResource(int amount)
    {
        UnityEngine.Debug.Log("ECONOMY RAISED!!");
        _owner.currentResource.Value += amount;   // nincs felsõ korlát
    }
    public void GainEconomyNextTurn(int value)
    {
        UnityEngine.Debug.Log("ECONOMY RAISED To next turn !!"+value.ToString());
        extraResourceNextTurn += value;
    }
    public int CurrentResource => _owner.currentResource.Value;
}