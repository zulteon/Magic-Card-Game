using UnityEngine;
using TMPro;
public class EffectTextSetterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    public void setText(string text)
    {
        descriptionText.text = text;
    }
}
