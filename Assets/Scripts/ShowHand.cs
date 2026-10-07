using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.XR;
using static Trigger;

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

    [Header("Ally hand automatikus leengedése")]
    [SerializeField] private bool autoLowerAllyHand = true;
    [Tooltip("Fix képernyõterület (0–1): X, Y, szélesség, magasság. Az origó bal alul van.")]
    [SerializeField] private Rect allyHoverArea = new Rect(0f, 0f, 1f, 0.2f);
    [SerializeField, Min(0f)] private float lowerDelay = 3f;
    [Tooltip("Ennyi helyi egységgel kerül lejjebb a handParent.")]
    [SerializeField, Min(0f)] private float loweredDistance = 0.2f;
    [Tooltip("A handParent eredeti méretének szorzója leengedve: 0.95 = 5%-kal kisebb.")]
    [SerializeField, Range(0.01f, 1f)] private float loweredScale = 0.95f;

    private float timeOutsideHand;
    private bool isHandLowered;
    private Vector3 handParentRestPosition;
    private Vector3 handParentRestScale;

    private void Awake()
    {
        if (handParent == null)
        {
            handParent = new GameObject("Hand Parent UI").transform;
            handParent.parent = transform;
        }

        handParentRestPosition = handParent.localPosition;
        handParentRestScale = handParent.localScale;
    }
    private void Start()
    {


        GameManager.instance.GetCardTemplates(out cardTemplateFront, out cardTemplateBack);
        getPlayer();
    }


    private void Update()
    {
        if (playerController == null || isEnemy || !autoLowerAllyHand)
        {
            timeOutsideHand = 0f;
            SetHandLowered(false);
            return;
        }

        if (IsMouseInsideHandArea())
        {
            timeOutsideHand = 0f;
            SetHandLowered(false);
        }
        else if (!isHandLowered)
        {
            timeOutsideHand += Time.unscaledDeltaTime;
            if (timeOutsideHand >= lowerDelay)
                SetHandLowered(true);
        }
    }

    private bool IsMouseInsideHandArea()
    {
        if (!Application.isFocused || Screen.width <= 0 || Screen.height <= 0)
            return false;

        Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return false;
        mousePosition = mouse.position.ReadValue();
#else
        mousePosition = Input.mousePosition;
#endif

        Vector2 normalizedPosition = new Vector2(
            mousePosition.x / Screen.width,
            mousePosition.y / Screen.height);

        return allyHoverArea.Contains(normalizedPosition);
    }

    private void SetHandLowered(bool lowered)
    {
        if (isHandLowered == lowered) return;

        isHandLowered = lowered;
        if (handParent == null) return;

        handParent.localPosition = handParentRestPosition
            + (lowered ? Vector3.down * loweredDistance : Vector3.zero);
        handParent.localScale = handParentRestScale * (lowered ? loweredScale : 1f);
    }

    private void OnDisable()
    {
        timeOutsideHand = 0f;
        SetHandLowered(false);

        if (playerController != null)
        {
            //playerController.hand.OnChange -= OnHandChanged;
        }
    }

    // TODO: A live PlayerController.hand eseményeirõl késõbb válasszuk le ezt a nézetet.
    // A külsõ feliratkozást / OnHandChanged-hívást is át kell majd kötni az új adatforrásra;
    // a getPlayer() jelenleg továbbra is a PlayerControllerbõl határozza meg az ally/enemy oldalt.
    public void OnHandChanged(SyncListOperation op, int index, CardState oldItem, CardState newItem, bool asServer)
    {

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
        else if (op == SyncListOperation.Set && index < handUI.Count)
        {
            //UpdateCardUI(index, newItem); a buff flesh event csinálja ezt.
        }
        /* else if (op == SyncListOperation.Complete)
         {
             foreach (var go in handUI) Destroy(go);
             handUI.Clear();

             for (int i = 0; i < playerController.hand.Count; i++)
                 CreateCardUI(playerController.hand[i]);

             ArrangeCards();
         } */
    }

    private void UpdateCardUI(int index, CardState state)
    {
        if (isEnemy) return;   // hátlapnál nincs mit frissíteni

        var view = handUI[index].GetComponent<CardView>();
        if (view == null) return;

        CardData cardData = CardManager.instance.GetCard(state.cardId);
        view.SetCard(cardData, state);
    }
    private void CreateCardUI(CardState state)
    {
        GameObject cardGO;
        // Lekérjük a statikus CardData-t a GameManager-bõl a CardState.cardId alapján
        CardData cardData = CardManager.instance.GetCard(state.cardId);
        if (cardData == null) { Debug.LogError($"Nincs ilyen cardId: {state.cardId}"); return; }
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
                view.SetCard(cardData, state);
            }
            else print("card is null");
        }

        handUI.Add(cardGO);
        ArrangeCards();
    }




    float margin = 0.5f;
    float cardSize = 1f;
    [SerializeField]
    float minusYHeight = -4f;
    [SerializeField]
    float YHeight = 4f;
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
            // Helyi koordináták: a handParent mozgatása és skálázása újrarendezéskor is érvényesül.
            card.transform.localPosition = new Vector3(x, y, -i * 0.1f); // Kis Z offset a rétegezéshez

            // Hearthstone-szerû forgatás: a kártya "néz" az ív érintõje irányába
            float rotationAngle = isEnemy ? angle : -angle; // Enemy-nél fordított irány
            card.transform.localRotation = Quaternion.Euler(0, 0, rotationAngle);

            // Opcionális: kártya méretezés (középsõ kártyák kicsit nagyobbak)
            float distanceFromCenter = Mathf.Abs(angle) / (totalAngle / 2f);
            float scale = Mathf.Lerp(1.0f, 0.9f, distanceFromCenter);
            card.transform.localScale = Vector3.one * scale;
        }
    }
    protected void getPlayer()
    {
        playerController = GetComponent<PlayerController>();
        isEnemy = !playerController.IsOwner;
    }
    public CardView FindCardView(ushort seqId)
    {
        for (int i = 0; i < handUI.Count; i++)
        {
            var view = handUI[i].GetComponent<CardView>();
            if (view != null && view.cardState.sequenceId == seqId)
                return view;
        }
        return null;
    }
    public void Hide(bool b=true)
    {
        handParent.gameObject.SetActive(!b);
    }
}
