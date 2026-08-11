using System.Diagnostics;

public class PlayerEconomy
{
    private PlayerController _owner;

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

        _owner.currentResource.Value = _owner.maxResource.Value;
    }

    public void RaiseResource(int amount)
    {
        UnityEngine.Debug.Log("ECONOMY RAISED!!");
        _owner.currentResource.Value += amount;   // nincs felsõ korlát
    }
    public int CurrentResource => _owner.currentResource.Value;
}