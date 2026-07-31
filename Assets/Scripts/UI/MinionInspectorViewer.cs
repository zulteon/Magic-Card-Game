using TMPro;
using UnityEngine;

public class MinionInspectView : MonoBehaviour
{
    public static MinionInspectView Instance;

    public GameObject content;

    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI nameText;

    public SpriteRenderer spriteRenderer;


    private void Awake()
    {
        Instance = this;
        if(content == null )content=transform.GetChild(0).gameObject;
        content.SetActive(false);
    }


    public void Show(MinionCard card, MinionState state)
    {
        content.SetActive(true);

        // AKTUÁLIS minion értékek
        attackText.text = state.attack.ToString();
        healthText.text = state.currentHealth.ToString();

        // Statikus kártyaadatok
        costText.text = card.cost.ToString();

        descriptionText.text =
            Translator.Translate(card.description);

        nameText.text =
            Translator.Translate(
                CardView.FormatSpriteToName(card.sprite)
            );

        spriteRenderer.sprite =
            Resources.Load<Sprite>(
                "Sprites/" + card.sprite
            );
    }


    public void Hide()
    {
        content.SetActive(false);
    }
}