using UnityEngine;

public class Hero :ScriptableObject
{
    public int Health = 0;
    public string type = "";
    public ILiveTarget.Race race;
}
