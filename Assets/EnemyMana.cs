using UnityEngine;
using TMPro;
public class EnemyMana : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ManaJarUI manaJarUI;

    public static EnemyMana instance;
    public TMP_Text txt;
    void Start()
    {
       instance = this;
    }

    // Update is called once per frame
    void Update()
    {   
        
    }
    internal void Fill(int mana)
    {
        float t=mana/10f;
        StartCoroutine(manaJarUI.FillTo(t, 0.3f));
        txt.text = mana.ToString();
    }
}
