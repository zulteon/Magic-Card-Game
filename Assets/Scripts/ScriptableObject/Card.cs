using UnityEngine;
[CreateAssetMenu(fileName = "Card", menuName = "Card Creator/Card", order = 1)]
public class Card : ScriptableObject
{
    public ushort cardId;
    public int Cost;
    public string description;
    public MinionData m;
    public Spell spell;
    public bool IsSpell()
    {
        if (spell == null && m == null)
            Debug.LogWarning("Null kártya nincs minion se spell" + name);   
        if (spell != null) return true;
        return false;
    }
}
