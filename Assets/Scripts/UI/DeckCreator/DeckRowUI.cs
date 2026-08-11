using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Egy sor a szöveges pakli-listában: költség, név, példányszám.</summary>
public class DeckRowUI : MonoBehaviour
{
    public TMP_Text costText;
    public TMP_Text nameText;
    public TMP_Text countText;

    private CardData _data;
    private System.Action<CardData> _onClick;

    private void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => _onClick?.Invoke(_data));
    }

    public void Bind(CardData data, int count, System.Action<CardData> onClick)
    {
        _data = data;
        _onClick = onClick;

        costText.text = data.cost.ToString();
        nameText.text = CardView.FormatSpriteToName(data.sprite);
        countText.text = count > 1 ? "x" + count : "";

        gameObject.SetActive(true);
    }
}