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
        UnityEngine.Debug.Log("Are we here ");
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

        GameManager.instance.SendClientEvent(new ClientEvent()
        {
            effectType = (ushort)Effect.Type.setManaCrystal,
            targetIds = new ushort[] { _owner.isEnemy.Value ? (ushort)1 : (ushort)0 },
            value = _owner.currentResource.Value,
        });
    }

    public void RaiseResource(int amount)
    {
        
        _owner.currentResource.Value += amount;   // nincs felsõ korlát
        EffectClient.instance.AddEvent(new ClientEvent()
        {
            effectType = (ushort)Effect.Type.setManaCrystal,
            targetIds = new ushort[] { _owner.isEnemy.Value ? (ushort)1 : (ushort)0 },
            value = _owner.currentResource.Value,
        });
    }
    public void GainEconomyNextTurn(int value)
    {
        UnityEngine.Debug.Log("ECONOMY RAISED To next turn !!"+value.ToString());
        extraResourceNextTurn += value;
    }
    public int CurrentResource => _owner.currentResource.Value;
}