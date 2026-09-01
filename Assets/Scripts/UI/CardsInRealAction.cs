using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private Coroutine _current;

    private void Awake()
    {
        instance = this;
        
        view.SetActive(false);
    }

    public void ShowCard(ushort cardId)
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

        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        view.SetActive(true);
        yield return new WaitForSeconds(displayDuration);
        view.SetActive(false);
        _current = null;
    }
}