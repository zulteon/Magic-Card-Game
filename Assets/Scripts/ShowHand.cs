using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.XR;

public class ShowHand : MonoBehaviour
{
    // Ez a lista a KLIENSEN létezõ UI GameObjet-eket tárolja
    public List<GameObject> handUI = new List<GameObject>();

    // Hivatkozás a PlayerController-re, hogy elérjük a hálózati listát
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
        print($"Új lap érkezett a kézbe: {newItem.cardId} (index {index}) op :{op.ToString()} isServer:{asServer}" );
        if (asServer)
            return;
        // A logikád itt továbbra is a tulajdonosi viszonyra épül, ami helyes.
        // A `asServer` paramétert használva elkerülheted a felesleges hívásokat.
        if (op == SyncListOperation.Add)
        {
            // Ha hozzáadódott egy új kártya, hozzuk létre a UI-t
            CreateCardUI(newItem);
        }
        else if (op == SyncListOperation.RemoveAt && index < handUI.Count)
        {
            // Ha eltávolítottak egy kártyát, pusztítsuk el a UI-t is
            Destroy(handUI[index]);
            handUI.RemoveAt(index);
            ArrangeCards();
        }
        else if (op == SyncListOperation.Clear)
        {
            // Ha a lista kiürül, töröljük az összes UI elemet
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

        // Lekérjük a statikus CardData-t a GameManager-bõl a CardState.cardId alapján
        Card cardData = GameManager.instance.GetCardById(cardState.cardId);
       // print("a kartya valtozott");
        if (isEnemy)
        {
            cardGO = Instantiate(cardTemplateBack, handParent);
            // Itt nem kell vizuális adatot beállítani, mert ez az ellenség keze
        }
        else
        {
            cardGO = Instantiate(cardTemplateFront, handParent);

            CardView view = cardGO.GetComponent<CardView>();
            if (view != null)
            {
                //print("loki loki "+cardData.m.attack);
                
                // A CardView a CardState-et és a statikus CardData-t is megkapja.
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

        // Hearthstone-szerû beállítások
        float radius = 8f; // Nagyobb radius természetesebb ívet ad
        float maxAngle = 45f; // Maximum szögtartomány (fél körív)
        float verticalCurve = 1.5f; // Függõleges ív mértéke

        // Szögek kiszámítása
        float totalAngle = Mathf.Min(maxAngle, count * 8f); // Max 45°, de függ a kártyák számától
        float angleStep = count > 1 ? totalAngle / (count - 1) : 0f;
        float startAngle = -totalAngle / 2f; // Középre igazítás

        float baseY = isEnemy ? YHeight : minusYHeight;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            float rad = Mathf.Deg2Rad * angle;

            // Ív menti pozíció számítása
            float x = Mathf.Sin(rad) * radius;
            float y = baseY + (Mathf.Cos(rad) - 1f) * verticalCurve; // -1f hogy lefelé íveljen

            var card = handUI[i];
            card.transform.position = new Vector3(x, y, -i * 0.1f); // Kis Z offset a rétegezéshez

            // Hearthstone-szerû forgatás: a kártya "néz" az ív érintõje irányába
            float rotationAngle = isEnemy ? angle : -angle; // Enemy-nél fordított irány
            card.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);

            // Opcionális: kártya méretezés (középsõ kártyák kicsit nagyobbak)
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
