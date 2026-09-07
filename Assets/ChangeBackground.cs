using UnityEngine;
public class ChangeBackground : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Sprite[] bgs;
    public static ChangeBackground instance;
    SpriteRenderer sp;
    void Awake()
    {
        instance = this;
        sp = GetComponent<SpriteRenderer>();
    }
    public void Change(int index,float t)
    {
        sp.sprite = bgs[index];
        sp.color = new Color(t, t, t, 1f);
    }
}
