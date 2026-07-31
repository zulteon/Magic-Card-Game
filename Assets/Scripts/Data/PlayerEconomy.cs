public class PlayerEconomy
{
    private PlayerController _owner;

    public PlayerEconomy(PlayerController owner)
    {
        _owner = owner;
    }

    public void RaiseResource(int amount)
    {
        _owner.currentResource.Value += amount;
    }

    public bool TrySpendResource(int amount)
    {
        if (_owner.currentResource.Value < amount) return false;
        _owner.currentResource.Value -= amount;
        return true;
    }

    public int CurrentResource => 3;//_owner.currentResource.Value;
}