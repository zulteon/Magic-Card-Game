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

    // Széles képnél ennyivel szorozzuk az eredeti méretet
    [SerializeField] private float wideImageScaleMultiplier = 0.8f;

    // Széles képnél ennyivel mozgatjuk fel Unity unitban
    [SerializeField] private float wideImageYOffset = 0.28f;

    private Vector3 originalSpriteScale;
    private Vector3 originalSpriteLocalPosition;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI nameText;
    private SpriteRenderer spriteRenderer;
    public ushort sequenceId;   // ÚJ

    private void OnMouseUp()
    {
        Debug.Log($"OnMouseUp: sequenceId={sequenceId}");
        onChosen?.Invoke(sequenceId);
    }  // sequenceId-t küldi, nem cardId-t
    private void Awake()
    {
        spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        originalSpriteScale = spriteRenderer.transform.localScale;
        originalSpriteLocalPosition = spriteRenderer.transform.localPosition;
    }
    private void Update()
    {
        AdjustSpriteImage();
    }

    public void SetCard(CardData cardData)
    {
        healthText.text = cardData is MinionCard m ? m.health.ToString() : "";
        attackText.text = cardData is MinionCard mm ? mm.attack.ToString() : "";
        costText.text = cardData.cost.ToString();
        descriptionText.text = cardData.description;
        nameText.text = cardData.cardName;
        spriteRenderer.sprite = Resources.Load<Sprite>("Sprites/" + cardData.sprite);
        AdjustSpriteImage();

    }
    private void AdjustSpriteImage()
    {
        if (spriteRenderer.sprite == null)
            return;

        // Elõször mindig visszaállítjuk az eredeti állapotot.
        // Ez azért kell, mert elõzõleg lehetett egy széles kép rajta.
        spriteRenderer.transform.localScale = originalSpriteScale;
        spriteRenderer.transform.localPosition = originalSpriteLocalPosition;

        Sprite sprite = spriteRenderer.sprite;

        // CSAK a széles képet módosítjuk
        if (sprite.rect.width > sprite.rect.height)
        {
            spriteRenderer.transform.localScale =
                new Vector3(
                    originalSpriteScale.x * wideImageScaleMultiplier,
                    originalSpriteScale.y * wideImageScaleMultiplier,
                    originalSpriteScale.z
                );

            Vector3 pos = originalSpriteLocalPosition;
            pos.y += wideImageYOffset;

            spriteRenderer.transform.localPosition = pos;
        }
    }

}
