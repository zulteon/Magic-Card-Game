using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MinionCardView : MonoBehaviour
{
    public static MinionCardView instance;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Card Data")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text nameText;

    [Header("Effects")]
    [SerializeField] private GameObject effectTextPrefab;
    [SerializeField] private Transform gridLayout;

    [Header("Position")]
    [SerializeField]
    private Vector3 offset =
        new Vector3(2.5f, 0f, -1f);

    private readonly List<EffectTextSetterUI> _effectTexts = new();

    private ushort _currentSequenceId;
    private Transform _target;


    private void Awake()
    {
        instance = this;

        if (root != null)
            root.SetActive(false);
    }


    

    // ═══════════════════════════════════════
    // SHOW
    // ═══════════════════════════════════════

    public void ShowCard(
        ushort cardId,
        ushort sequenceId,
        Transform target)
    {
        _currentSequenceId = sequenceId;
        _target = target;

        // Régi minion effektjeit eltávolítjuk.
        ClearEffects();

        CardData cardData =
            CardManager.instance.GetCard(cardId);

        if (cardData == null)
        {
            Debug.LogWarning(
                $"MinionCardView: cardId {cardId} not found."
            );

            return;
        }

        SetCard(cardData);

        if (root != null)
            root.SetActive(true);

        UpdatePosition();
    }


    // ═══════════════════════════════════════
    // CARD DATA
    // ═══════════════════════════════════════

    private void SetCard(CardData cardData)
    {
        if (cardData == null)
            return;

        nameText.text = cardData.cardName;
        descriptionText.text = cardData.description;
        costText.text = cardData.cost.ToString();

        if (cardData is MinionCard minion)
        {
            attackText.text =
                minion.attack.ToString();

            healthText.text =
                minion.health.ToString();
        }
        else
        {
            attackText.text = "";
            healthText.text = "";
        }

        LoadSprite(cardData);
    }


    private void LoadSprite(CardData cardData)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite =
            Resources.Load<Sprite>(
                "Sprites/" + cardData.sprite
            );
    }


    // ═══════════════════════════════════════
    // EFFECTS FROM SERVER
    // ═══════════════════════════════════════

    public void ReceiveMinionEffects(
        ushort sequenceId,
        string[] effects)
    {
        // Lehet, hogy közben már másik
        // minion fölé ment az egér.
        if (sequenceId != _currentSequenceId)
            return;

        ClearEffects();

        if (effects == null || effects.Length == 0)
        {
            //CreateEffectText("No active effects.");
            return;
        }

        foreach (string effect in effects)
        {
            if (string.IsNullOrWhiteSpace(effect))
                continue;

            CreateEffectText(effect);
        }
    }


    // ═══════════════════════════════════════
    // EFFECT UI
    // ═══════════════════════════════════════

    private void CreateEffectText(string text)
    {
        

        EffectTextSetterUI effectUI =
            Instantiate(
                effectTextPrefab,
                gridLayout
            ).GetComponent<EffectTextSetterUI>();
        effectUI.gameObject.SetActive(true);

        effectUI.setText(text);

        _effectTexts.Add(effectUI);
    }


    private void ClearEffects()
    {
        for (int i = 0; i < _effectTexts.Count; i++)
        {
            if (_effectTexts[i] != null)
                Destroy(_effectTexts[i].gameObject);
        }

        _effectTexts.Clear();
    }


    // ═══════════════════════════════════════
    // HIDE
    // ═══════════════════════════════════════

    public void Hide(ushort sequenceId)
    {
        // Egy régebbi minion OnMouseExit-je
        // ne tudja bezárni az új preview-t.
        if (sequenceId != _currentSequenceId)
            return;

        _target = null;

        ClearEffects();

        if (root != null)
            root.SetActive(false);
    }


    // ═══════════════════════════════════════
    // POSITION
    // ═══════════════════════════════════════

    private void UpdatePosition()
    {
        if (_target == null)
            return;

        Vector3 usedOffset = offset;

        if (Camera.main != null)
        {
            Vector3 screenPosition =
                Camera.main.WorldToScreenPoint(
                    _target.position
                );

            // Jobb oldalon lévő minion esetén
            // bal oldalra tesszük a preview-t.
            if (screenPosition.x >
                Screen.width * 0.5f)
            {
                usedOffset.x =
                    -Mathf.Abs(offset.x);
            }
            else
            {
                usedOffset.x =
                    Mathf.Abs(offset.x);
            }
        }

        transform.position =
            _target.position + usedOffset;

        transform.rotation =
            Quaternion.identity;
    }


    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    public Transform[] minionBorders;
}