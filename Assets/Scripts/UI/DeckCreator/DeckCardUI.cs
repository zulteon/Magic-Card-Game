using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;
/// <summary>
/// Egy kártya a rácsban. A DeckBuilderUI tölti fel adattal és köti be a kattintást.
/// A prefab Canvas alatt él (UI Image, nem SpriteRenderer).
/// </summary>
public class DeckCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referenciák")]
    public Image portrait;
    public TMP_Text costText;
    public TMP_Text nameText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public GameObject countBadge;
    public TMP_Text countText;
    public TMP_Text descriptionText;

    [Header("Hover")]
    public float hoverScale = 1.08f;

    private RectTransform _rt;
    private CardData _data;
    private System.Action<CardData> _onClick;

    public CardData Data => _data;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();

        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => _onClick?.Invoke(_data));
    }
    public  string FormatSpriteToName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return "Unknown Card";

        // 1. Szóköz beszúrása a nagybetûk elé (pl: LiquidAngel -> Liquid Angel)
        string result = Regex.Replace(spriteName, "([a-z])([A-Z])", "$1 $2");

        // 2. Az elsõ betû biztosan nagybetû legyen (pl: crystalzombie -> Crystalzombie)
        result = char.ToUpper(result[0]) + result.Substring(1);

        return result;
    }
    // CardData
    [SerializeField] float wideIMGScale=3.11f;

    public void Bind(CardData data, int countInDeck, System.Action<CardData> onClick)
    {
        _data = data;
        _onClick = onClick;

        nameText.text = data.cardName;
        costText.text = data.cost.ToString();

        if (data is MinionCard m)
        {
            attackText.text = $"{m.attack}";
            healthText.text = $"{m.health}";
            attackText.gameObject.SetActive(true);
            healthText.gameObject.SetActive(true);
        }
        else
        {
            attackText.gameObject.SetActive(false);
            healthText.gameObject.SetActive(false);
        }

        descriptionText.text = data.description;

        portrait.sprite = Resources.Load<Sprite>("Sprites/" + data.sprite);
        portrait.enabled = portrait.sprite != null;

        if (portrait.sprite != null)
        {
            // Ha a kép szélesebb, mint amilyen magas
            if (portrait.sprite.rect.width > portrait.sprite.rect.height)
            {
                portrait.transform.localScale =new Vector3(wideIMGScale, wideIMGScale, 1);
            }
        }

        SetCount(countInDeck);
        gameObject.SetActive(true);
    }

    public void SetCount(int count)
    {
        bool show = count > 0;
        if (countBadge != null) countBadge.SetActive(show);
        if (countText != null && show) countText.text = "x" + count;
    }

    public void Hide() => gameObject.SetActive(false);

    public void OnPointerEnter(PointerEventData e) => _rt.localScale = Vector3.one * hoverScale;
    public void OnPointerExit(PointerEventData e) => _rt.localScale = Vector3.one;
}