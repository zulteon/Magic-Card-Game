using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class CardInRealAction : MonoBehaviour
{
    public static CardInRealAction instance;

    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private SpriteRenderer cardSprite;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField]
    GameObject view;

    [SerializeField] private TextMeshProUGUI victimAttackText;
    [SerializeField] private TextMeshProUGUI victimHealthText;
    [SerializeField] private TextMeshProUGUI victimCostText;
    [SerializeField] private TextMeshProUGUI victimDescriptionText;
    [SerializeField] private TextMeshProUGUI victimNameText;
    [SerializeField] private SpriteRenderer victimCardSprite;
    public GameObject victim;

    private Coroutine _current;

    private void Awake()
    {
        instance = this;
        
        view.SetActive(false);
    }

    public void ShowCard(ushort cardId,int targetIds)
    {
        var cardData = CardManager.instance.GetCard(cardId);
        if (cardData == null) return;

        bool isMinion = cardData is MinionCard;
        attackText.text = isMinion ? ((MinionCard)cardData).attack.ToString() : "";
        healthText.text = isMinion ? ((MinionCard)cardData).health.ToString() : "";
        costText.text = cardData.cost.ToString();
        descriptionText.text = cardData.description;
        nameText.text = cardData.cardName;

        if (!string.IsNullOrEmpty(cardData.sprite))
        {
            var spriteName = cardData.sprite.Replace(".png", "");
            cardSprite.sprite = Resources.Load<Sprite>("Sprites/" + spriteName);
        }
        print("try to showing victim " + targetIds);
        RevealVictim((ushort)targetIds);
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ShowRoutine());
    }
    public void RevealVictim(ushort id)
    {if (id < 2) {
            victim.gameObject.SetActive(false);
            return; }
        MinionState card = GameManager.instance.GetMinionById(id);
        var cardData = CardManager.instance.GetCard(card.cardId);
        if (cardData == null) return;
        victim.gameObject.SetActive(true);
        victimAttackText.text = card.attack.ToString();
        victimHealthText.text = card.currentHealth.ToString();
        victimCostText.text = cardData.cost.ToString();
        victimDescriptionText.text = cardData.description;
        victimNameText.text = cardData.cardName;

        victimCardSprite.sprite = Resources.Load<Sprite>("Sprites/" +cardData.sprite);

    }
    private IEnumerator ShowRoutine()
    {
        view.SetActive(true);
        yield return new WaitForSeconds(displayDuration);
        view.SetActive(false);
        victim.SetActive(false);
        _current = null;
    }
}