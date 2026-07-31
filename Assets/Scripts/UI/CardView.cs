using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;
using static Trigger;
using System.Collections;
using System.Collections.Generic;
public class CardView : MonoBehaviour
{
    
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI nameText;
    bool isEnemy;
    public CardState cardState;
    SpriteRenderer spriteRenderer;
    public void Awake()
    {
        spriteRenderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        _atkBaseSize = attackText.fontSize;
        _hpBaseSize = healthText.fontSize;
    }
    public void SetCard(CardData cardData,CardState cardState, bool isEnemy = false)
    {
        this.cardState = cardState;
        /// kilehetne minden tárolást törölni és direkt beálitani az értékeket
        // A CardView statikusan tárolja az adatokat
        this.isEnemy = isEnemy;
       // 

        // A kártya GameObject-je
        if (isEnemy)
        {
            // Ha az ellenség kártyája, csak a hátlapot mutatjuk
            return;
        }

        // Dinamikus adatok a CardState-bõl
        // A cardData.isSpell-t használjuk a feltételhez
        healthText.text = cardData is MinionCard minion
                ? (minion.health + cardState.healthBonus).ToString() : "";

        attackText.text = cardData is MinionCard m
                ? (m.attack + cardState.attackBonus).ToString() : "";

        // A kártya aktuális költségét a CardState.currentCost mezõbõl kapjuk
        costText.text = cardState.currentCost.ToString();

        // Statikus adatok a CardData-ból
        descriptionText.text = cardData.description;
        nameText.text = FormatSpriteToName(cardData.sprite);
        LoadSprite(cardData);

    }
    Vector3 mousePosition;
    float moveSpeed=0.03f;
    private void OnMouseDrag()
    {
        
            mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition) + new Vector3(0, 0, 3);
            transform.position = Vector3.Lerp(transform.position, mousePosition, moveSpeed);
            
        
    }
    float playingMinHeight = -2.1f;
    private void OnMouseUp()
    {
        if(transform.position.y > playingMinHeight)
        {
            
            if (!GameManager.instance.IsMyTurn())
            {
                GameManager.instance.GetPlayer().showHand.ArrangeCards();
            }
            else//Play card
            {
                
            }
        }
        else
        {
            GameManager.instance.GetPlayer().showHand.ArrangeCards();
        }
   
    }
    private void OnMouseOver()
    {
        if(!isEnemy)
        transform.localScale = new Vector3(1.2f, 1.2f, 1);
    }
    private void OnMouseExit()
    {
        if(!isEnemy)
        transform.localScale= new Vector3(1, 1, 1);
    }
    public void LoadSprite(CardData card)
    {
         spriteRenderer.sprite=Resources.Load<Sprite>("Sprites/" + card.sprite);
    }
    #region FlashEffect
    private Coroutine _flash;
    private float _atkBaseSize, _hpBaseSize;

    // Awake végére:
    

public Coroutine PlayBuffFlash(int newAttack, int newHealth)
    {
        attackText.text = newAttack.ToString();
        healthText.text = newHealth.ToString();

        if (_flash != null) StopCoroutine(_flash);
        _flash = StartCoroutine(FlashRoutine());
        return _flash;
    }

    private IEnumerator FlashRoutine()
    {
        const float dur = 0.3f;
        const float grow = 1.6f;

        attackText.color = healthText.color = Color.green;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            float size = Mathf.Lerp(grow, 1f, k);
            attackText.fontSize = _atkBaseSize * size;
            healthText.fontSize = _hpBaseSize * size;

            Color c = Color.Lerp(Color.green, Color.white, k);
            attackText.color = healthText.color = c;

            yield return null;
        }

        attackText.fontSize = _atkBaseSize;
        healthText.fontSize = _hpBaseSize;
        attackText.color = healthText.color = Color.white;
        _flash = null;
    }
    #endregion
    public static string FormatSpriteToName(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return "Unknown Card";

        // 1. Szóköz beszúrása a nagybetûk elé (pl: LiquidAngel -> Liquid Angel)
        string result = Regex.Replace(spriteName, "([a-z])([A-Z])", "$1 $2");

        // 2. Az elsõ betû biztosan nagybetû legyen (pl: crystalzombie -> Crystalzombie)
        result = char.ToUpper(result[0]) + result.Substring(1);

        return result;
    }
    
}


