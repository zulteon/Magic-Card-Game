using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pakli-szerkesztő. Lapozós rács (nem scroll), mellette szöveges pakli-lista.
/// Az oszlopszám a rendelkezésre álló szélességből számolódik.
/// </summary>
public class DeckBuilderUI : MonoBehaviour
{
    [Header("Rács")]
    public RectTransform gridArea;          // a terület, ahova a kártyák kerülnek
    public DeckCardUI cardPrefab;
    public Vector2 cardSize = new Vector2(180f, 260f);
    public Vector2 margin = new Vector2(16f, 16f);
    [SerializeField] private int rows = 2;

    [Header("Lapozás")]
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageText;

    [Header("Kereső")]
    public TMP_InputField searchField;

    [Header("Pakli")]
    public TMP_InputField deckNameField;
    public RectTransform deckListArea;      // szöveges sorok szülője
    public DeckRowUI deckRowPrefab;
    public TMP_Text deckCountText;

    [Header("Gombok")]
    public Button saveButton;
    public Button loadButton;
    public Button newButton;
    public Button editModeButton;
    public Button doneButton;
    public TMP_Text editModeText;

    [Header("Szabályok")]
    public int minDeckSize = 25;
    public int maxDeckSize = 35;
    public int maxCopies = 2;

    // ── állapot ──
    private readonly List<DeckCardUI> _slots = new();
    private readonly List<DeckRowUI> _deckRows = new();

    private List<CardData> _filtered = new();
    private List<CardData> _deck = new();          // a pakli: minden példány külön elem
    private string _deckName = "Új pakli";

    private int _page;
    private bool _editMode;                        // false = gyűjtemény, true = a pakli lapjai

    private int Columns => Mathf.Max(1,
        Mathf.FloorToInt((gridArea.rect.width + margin.x) / (cardSize.x + margin.x)));

    private int PerPage => Columns * rows;
    private List<CardData> _allCards = new();
    private int PageCount => Mathf.Max(1, Mathf.CeilToInt(_filtered.Count / (float)PerPage));

    private IEnumerator Start()
    {
        searchField.onValueChanged.AddListener(_ => { _page = 0; Refresh(); });

        prevButton.onClick.AddListener(() => { _page = Mathf.Max(0, _page - 1); Refresh(); });
        nextButton.onClick.AddListener(() => { _page = Mathf.Min(PageCount - 1, _page + 1); Refresh(); });

        saveButton.onClick.AddListener(SaveDeck);
        newButton.onClick.AddListener(NewDeck);
        editModeButton.onClick.AddListener(ToggleEditMode);
        try { doneButton.onClick.AddListener(SceneManagement.instance.OpenMainMenu); }
        catch { Debug.LogWarning("nem tudtam hozzá adni a done buttonhoz"); }

        deckNameField.text = _deckName;

        loadButton.onClick.AddListener(OnLoadClicked);
        RefreshDeckDropdown();

        // A gridArea szélessége az első képkockában még 0 lehet (layout előtt).
        yield return null;


        Refresh();
        Refresh();
    }
    #region Load
    [Header("Betöltés")]
    public TMP_Dropdown deckDropdown;

    // Start-ban:
    private void RefreshDeckDropdown()
    {
        var names = DeckStorage.ListDecks();
        deckDropdown.ClearOptions();
        deckDropdown.AddOptions(names);
        loadButton.interactable = names.Count > 0;
    }

    private void OnLoadClicked()
    {
        if (deckDropdown.options.Count == 0) return;
        LoadDeck(deckDropdown.options[deckDropdown.value].text);
    }
    #endregion
    private void ToggleEditMode()
    {
        _editMode = !_editMode;
        _page = 0;
        editModeText.text = _editMode ? "Gyűjtemény" : "Pakli szerkesztése";
        Refresh();
    }

    // ═════════ MEGJELENÍTÉS ═════════

    private void Refresh()
    {
        _filtered = _editMode
            ? DeckAsCardList()
            : CardManager.instance.Search(searchField.text);

        _page = Mathf.Clamp(_page, 0, PageCount - 1);

        EnsureSlots(PerPage);
        LayoutSlots();

        int start = _page * PerPage;

        for (int i = 0; i < _slots.Count; i++)
        {
            int index = start + i;

            if (index >= _filtered.Count) { _slots[i].Hide(); continue; }

            CardData data = _filtered[index];
            _slots[i].Bind(data, CountInDeck(data.cardId), OnCardClicked);
        }

        pageText.text = $"{_page + 1} / {PageCount}";
        prevButton.interactable = _page > 0;
        nextButton.interactable = _page < PageCount - 1;

        RefreshDeckList();
    }

    /// <summary>A pakli lapjai, duplikátumok nélkül — a szerkesztő módhoz.</summary>
    private List<CardData> DeckAsCardList() =>
        _deck.GroupBy(c => c.cardId).Select(g => g.First()).OrderBy(c => c.cost).ToList();

    private void EnsureSlots(int needed)
    {
        while (_slots.Count < needed)
        {
            DeckCardUI card =
                Instantiate(    
                    cardPrefab,
                    gridArea,
                    false
                );

            _slots.Add(card);
        }

        for (int i = needed; i < _slots.Count; i++)
            _slots[i].Hide();
    }
    /// <summary>Balról jobbra, felülről lefelé, a gridArea közepéből kiindulva.</summary>
    private void LayoutSlots()
    {
        int cols = Columns;

        float totalW = cols * cardSize.x + (cols - 1) * margin.x;
        float startX = -totalW / 2f + cardSize.x / 2f;

        float totalH = rows * cardSize.y + (rows - 1) * margin.y;
        float startY = totalH / 2f - cardSize.y / 2f;

        for (int i = 0; i < _slots.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;

            var rt = _slots[i].GetComponent<RectTransform>();
            rt.sizeDelta = cardSize;
            rt.anchoredPosition = new Vector2(
                startX + col * (cardSize.x + margin.x),
                startY - row * (cardSize.y + margin.y));
        }
    }

    // ═════════ PAKLI-MŰVELETEK ═════════

    private void OnCardClicked(CardData data)
    {
        if (data == null) return;

        if (_editMode) RemoveFromDeck(data.cardId);
        else AddToDeck(data);

        Refresh();
    }

    private int CountInDeck(ushort cardId) => _deck.Count(c => c.cardId == cardId);

    private void AddToDeck(CardData card)
    {
        if (_deck.Count >= maxDeckSize)
        {
            Debug.Log($"A pakli tele van ({maxDeckSize}).");
            return;
        }

        if (CountInDeck(card.cardId) >= maxCopies)
        {
            Debug.Log($"Legfeljebb {maxCopies} példány lehet egy lapból.");
            return;
        }

        _deck.Add(card);
    }

    private void RemoveFromDeck(ushort cardId)
    {
        int i = _deck.FindIndex(c => c.cardId == cardId);
        if (i >= 0) _deck.RemoveAt(i);
    }

    // ═════════ SZÖVEGES PAKLI-LISTA ═════════

    private void RefreshDeckList()
    {
        var grouped = _deck
            .GroupBy(c => c.cardId)
            .Select(g => (card: g.First(), count: g.Count()))
            .OrderBy(x => x.card.cost)
            .ThenBy(x => x.card.cardName)
            .ToList();

        while (_deckRows.Count < grouped.Count)
            _deckRows.Add(Instantiate(deckRowPrefab, deckListArea));

        for (int i = 0; i < _deckRows.Count; i++)
        {
            if (i >= grouped.Count) { _deckRows[i].gameObject.SetActive(false); continue; }

            var (card, count) = grouped[i];
            _deckRows[i].Bind(card, count, OnDeckRowClicked);
        }

        deckCountText.text = $"{_deck.Count} / {minDeckSize}";
        deckCountText.color = IsDeckValid() ? Color.white : new Color(0.8f, 0.4f, 0.3f);
    }

    /// <summary>Sorra kattintás: ugrás arra a lapra a rácsban.</summary>
    private void OnDeckRowClicked(CardData card)
    {
        int index = _filtered.FindIndex(c => c.cardId == card.cardId);

        if (index < 0)
        {
            searchField.text = "";                      // a kereső elrejthette
            _filtered = _editMode ? DeckAsCardList() : CardManager.instance.Search("");
            index = _filtered.FindIndex(c => c.cardId == card.cardId);
        }

        if (index < 0) return;

        _page = index / PerPage;
        Refresh();
    }

    // ═════════ MENTÉS / BETÖLTÉS ═════════

    private bool IsDeckValid() => _deck.Count >= minDeckSize && _deck.Count <= maxDeckSize;

    private void SaveDeck()
    {
        if (!IsDeckValid())
        {
            Debug.LogWarning($"A pakli {minDeckSize}-{maxDeckSize} lap között kell legyen. " +
                             $"Jelenleg: {_deck.Count}");
            return;
        }

        _deckName = string.IsNullOrWhiteSpace(deckNameField.text)
            ? "Névtelen pakli" : deckNameField.text;

        DeckStorage.Save(_deckName, _deck);
        RefreshDeckDropdown();
    }

    /// <summary>Kívülről hívható: egy mentett pakli betöltése szerkesztésre.</summary>
    public void LoadDeck(string deckName)
    {
        _deck = DeckStorage.Load(deckName);
        _deckName = deckName;
        deckNameField.text = deckName;
        _page = 0;
        Refresh();
    }

    private void NewDeck()
    {
        _deck.Clear();
        _deckName = "Új pakli";
        deckNameField.text = _deckName;
        _editMode = false;
        _page = 0;
        Refresh();
    }
}