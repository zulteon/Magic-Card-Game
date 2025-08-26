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
        /// kilehetne minden t�rol�st t�r�lni �s direkt be�litani az �rt�keket
        // A CardView statikusan t�rolja az adatokat
        this.isEnemy = isEnemy;

        // A k�rtya GameObject-je
        if (isEnemy)
        {
            // Ha az ellens�g k�rty�ja, csak a h�tlapot mutatjuk
            return;
        }

        // Dinamikus adatok a CardState-b�l
        // A cardData.isSpell-t haszn�ljuk a felt�telhez
        attackText.text = cardData.IsSpell() ? "" : cardData.m.attack.ToString();
        healthText.text = cardData.IsSpell() ? "" : cardData.m.health.ToString();

        // A k�rtya aktu�lis k�lts�g�t a CardState.currentCost mez�b�l kapjuk
        costText.text = cardState.currentCost.ToString();

        // Statikus adatok a CardData-b�l
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
