using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Spell", menuName = "Card Creator/Spell")]
public class Spell:ScriptableObject
{
    
    public string sprite;
    public string description;
    public string effect;
    public List<Effect> e;
}
