using System;
using System.Diagnostics;

public class MinionLogic
{
    public ushort _sequenceId;
    bool sleep;
    bool frozen;
    private MinionState State
    {
        get => GameManager.instance.GetMinionById(_sequenceId);
        set => GameManager.instance.ChangeMinionById(_sequenceId,minion=> minion=value);
    }

    public MinionLogic(ushort sequenceId)
    {
        _sequenceId = sequenceId;
    }

    public void Damage(int damage)
    {
        var state = State;
        state.currentHealth = (ushort)UnityEngine.Mathf.Max(0, state.currentHealth - damage);
        if (state.currentHealth < 1)
        {
            Die();
        }
        UnityEngine.Debug.Log("damaged" +damage+ "health" +state.currentHealth);
      //  GameManager.instance.ChangeMinionById(_sequenceId, minion => minion.currentHealth = state.currentHealth);
        State = state; // automatikusan mentõdik
    }
    public void Sleep(bool sleep = true)
    {
        this.sleep = sleep;
    }
    public void Freeze(bool freeze = true)
    {
        this.frozen = freeze;
    }
    private void Die()
    {
        //send to gamemanager. but open graveyard in game manager if multiple actions ... 
    }

    internal void Heal(int value)
    {
        throw new NotImplementedException();
    }
}