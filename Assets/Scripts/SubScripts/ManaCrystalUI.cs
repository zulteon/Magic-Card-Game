using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class ManaCrystalUI : MonoBehaviour
{
    public Image manaCrystallfill;
    public TextMeshProUGUI manaText;
    public static ManaCrystalUI instance;
    public void setManaCrystal(int manaCrystal,int maxMana)
    {
        manaCrystallfill.fillAmount = (float)manaCrystal / maxMana;
        manaText.text = manaCrystal.ToString()+" | "+maxMana.ToString();
    }
    void Awake()
    {
        instance = this;
    }
}
