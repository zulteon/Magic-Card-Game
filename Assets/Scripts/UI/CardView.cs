using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI nameText;
    bool isEnemy;
    public CardState cardState;
    public void SetCard(Card cardData,CardState cardState, bool isEnemy = false)
    {
        this.cardState = cardState;
        /// kilehetne minden tárolást törölni és direkt beálitani az értékeket
        // A CardView statikusan tárolja az adatokat
        this.isEnemy = isEnemy;

        // A kártya GameObject-je
        if (isEnemy)
        {
            // Ha az ellenség kártyája, csak a hátlapot mutatjuk
            return;
        }

        // Dinamikus adatok a CardState-bõl
        // A cardData.isSpell-t használjuk a feltételhez
        attackText.text = cardData.IsSpell() ? "" : cardData.m.attack.ToString();
        healthText.text = cardData.IsSpell() ? "" : cardData.m.health.ToString();

        // A kártya aktuális költségét a CardState.currentCost mezõbõl kapjuk
        costText.text = cardState.currentCost.ToString();

        // Statikus adatok a CardData-ból
        descriptionText.text = cardData.description;
        nameText.text = cardData.name;

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
}
