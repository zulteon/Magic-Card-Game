using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.XR;

public class ShowHand : MonoBehaviour
{
    // Ez a lista a KLIENSEN l�tez� UI GameObjet-eket t�rolja
    public List<GameObject> handUI = new List<GameObject>();

    // Hivatkoz�s a PlayerController-re, hogy el�rj�k a h�l�zati list�t
    public PlayerController playerController;
    public GameObject cardTemplateFront;
    public GameObject cardTemplateBack;
    public Transform handParent;
    public bool isEnemy { get; set; }

    private void Start()
    {

        handParent = new GameObject("Hand Parent UI").transform;
        handParent.transform.parent = transform;
        playerController = GameManager.instance.GetLocalPlayerController();
        GameManager.instance.GetCardTemplates(out cardTemplateFront, out cardTemplateBack);
        getPlayer();
    }
    
    
    private void OnDisable()
    {
        if (playerController != null)
        {
            //playerController.hand.OnChange -= OnHandChanged;
        }
    }

    public void OnHandChanged(SyncListOperation op, int index, CardState oldItem, CardState newItem, bool asServer)
    {
        print($"�j lap �rkezett a k�zbe: {newItem.cardId} (index {index}) op :{op.ToString()} isServer:{asServer}" );
        if (asServer)
            return;
        // A logik�d itt tov�bbra is a tulajdonosi viszonyra �p�l, ami helyes.
        // A `asServer` param�tert haszn�lva elker�lheted a felesleges h�v�sokat.
        if (op == SyncListOperation.Add)
        {
            // Ha hozz�ad�dott egy �j k�rtya, hozzuk l�tre a UI-t
            CreateCardUI(newItem);
        }
        else if (op == SyncListOperation.RemoveAt && index < handUI.Count)
        {
            // Ha elt�vol�tottak egy k�rty�t, puszt�tsuk el a UI-t is
            Destroy(handUI[index]);
            handUI.RemoveAt(index);
            ArrangeCards();
        }
        else if (op == SyncListOperation.Clear)
        {
            // Ha a lista ki�r�l, t�r�lj�k az �sszes UI elemet
            foreach (var cardUI in handUI)
            {
                Destroy(cardUI);
            }
            handUI.Clear();
        }
    }


    private void CreateCardUI(CardState cardState)
    {
        GameObject cardGO;

        // Lek�rj�k a statikus CardData-t a GameManager-b�l a CardState.cardId alapj�n
        Card cardData = GameManager.instance.GetCardById(cardState.cardId);
       // print("a kartya valtozott");
        if (isEnemy)
        {
            cardGO = Instantiate(cardTemplateBack, handParent);
            // Itt nem kell vizu�lis adatot be�ll�tani, mert ez az ellens�g keze
        }
        else
        {
            cardGO = Instantiate(cardTemplateFront, handParent);

            CardView view = cardGO.GetComponent<CardView>();
            if (view != null)
            {
                //print("loki loki "+cardData.m.attack);
                
                // A CardView a CardState-et �s a statikus CardData-t is megkapja.
                view.SetCard( cardData,cardState);
            }
        }

        handUI.Add(cardGO);
        ArrangeCards();
    }




    float margin = 0.5f;
    float cardSize = 1f;
    float minusYHeight=-4f;
    float YHeight=4f;
    public void ArrangeCards()
    {
        int count = handUI.Count;

        if (count == 0) return;

        // Hearthstone-szer� be�ll�t�sok
        float radius = 8f; // Nagyobb radius term�szetesebb �vet ad
        float maxAngle = 45f; // Maximum sz�gtartom�ny (f�l k�r�v)
        float verticalCurve = 1.5f; // F�gg�leges �v m�rt�ke

        // Sz�gek kisz�m�t�sa
        float totalAngle = Mathf.Min(maxAngle, count * 8f); // Max 45�, de f�gg a k�rty�k sz�m�t�l
        float angleStep = count > 1 ? totalAngle / (count - 1) : 0f;
        float startAngle = -totalAngle / 2f; // K�z�pre igaz�t�s

        float baseY = isEnemy ? YHeight : minusYHeight;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            float rad = Mathf.Deg2Rad * angle;

            // �v menti poz�ci� sz�m�t�sa
            float x = Mathf.Sin(rad) * radius;
            float y = baseY + (Mathf.Cos(rad) - 1f) * verticalCurve; // -1f hogy lefel� �veljen

            var card = handUI[i];
            card.transform.position = new Vector3(x, y, -i * 0.1f); // Kis Z offset a r�tegez�shez

            // Hearthstone-szer� forgat�s: a k�rtya "n�z" az �v �rint�je ir�ny�ba
            float rotationAngle = isEnemy ? angle : -angle; // Enemy-n�l ford�tott ir�ny
            card.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);

            // Opcion�lis: k�rtya m�retez�s (k�z�ps� k�rty�k kicsit nagyobbak)
            float distanceFromCenter = Mathf.Abs(angle) / (totalAngle / 2f);
            float scale = Mathf.Lerp(1.0f, 0.9f, distanceFromCenter);
            card.transform.localScale = Vector3.one * scale;
        }
    }
    protected void getPlayer()
    {
        playerController=GetComponent<PlayerController>();
    }
}
