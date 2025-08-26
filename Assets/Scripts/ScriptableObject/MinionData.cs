using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "Minion", menuName = "Card Creator/new Minion")]
public class MinionData : ScriptableObject
{
    

    public int attack, health;
    public string sprite;
    public string description;
    public string effect;
    public List<Effect> e;
    public ILiveTarget.Race race;
}
