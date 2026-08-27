using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;
using static Trigger;
using System.Collections;
using System.Collections.Generic;
public class DiscoverCardView : MonoBehaviour
{
    public ushort cardId;
    public System.Action<ushort> onChosen;

    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI nameText;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
    }

    public void SetCard(CardData cardData)
    {
        healthText.text = cardData is MinionCard m ? m.health.ToString() : "";
        attackText.text = cardData is MinionCard mm ? mm.attack.ToString() : "";
        costText.text = cardData.cost.ToString();
        descriptionText.text = cardData.description;
        nameText.text = CardView.FormatSpriteToName(cardData.sprite);
        spriteRenderer.sprite = Resources.Load<Sprite>("Sprites/" + cardData.sprite);
    }

    private void OnMouseUp() => onChosen?.Invoke(cardId);
}
