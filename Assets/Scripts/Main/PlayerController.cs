using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Timing;

public class PlayerController : NetworkBehaviour
{
    // A SyncList automatikusan szinkronizálja a szerveroldali változásokat a kliensekkel.
    public readonly SyncList<ushort> deck = new ();
    public readonly SyncList<CardState> hand = new ();// saját oldalon lévõ minionok
    private readonly List<CardState> _serverDeckData = new();
    // Itt tároljuk a kártyák aktuális állapotát a pakliban
    //private Dictionary<string, CardState> cardStates = new Dictionary<string, CardState>();
    /*Ez a LiveCard (dinamikus állapot) és a Card (statikus adat) közötti megkülönböztetés kulcsfontosságú.
     * A SyncList-ednek valahogy a buffokat is tárolnia kell, ha a kártyák visszakerülnek a pakliba.
     */
    BoardManager boardManager;

    public bool isDummy { set;get; }
    Transform NetworkCards_Hand;
    Transform NetworkCards_Board;
    
    private void Start()
    {
       NetworkCards_Hand= new GameObject("HandNetworkCardData").transform;
       NetworkCards_Hand.transform.parent = transform;
       NetworkCards_Board = new GameObject("BoardNetworkCardData").transform;
       NetworkCards_Board.transform.parent = transform;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && !isEnemy&&IsClient)
        {
            // print(hand[0].c);
            CmdPlayMinion(hand[0]);
        }
    }
    
    public GameObject liveCardPrefab;
    public LiveHero hero { get; set; }
    int Health;
    public ShowHand showHand;
    public GameManager manager;
    public bool isEnemy;
    public PlayerState state;

    private bool _subscribed = false;

    public void Init(PlayerState player,bool home,GameManager manager2)
    {
        if (manager == null)
            manager = manager2;
        boardManager=manager.gameObject.GetComponent<BoardManager>();
        Debug.Log("This is enemy: " + isEnemy + " is this player a?" + manager.isAlly(this) + "are we client? " + IsClient+"Is this haho");
        liveCardPrefab = manager.liveCardPrefab;
        CreateDeck(player.deck);
        
        hero =gameObject.GetComponent<LiveHero>();
        showHand = gameObject.GetComponent<ShowHand>();
        if (hero==null)
            hero =gameObject.AddComponent<LiveHero>();
        if(showHand==null)
            showHand=gameObject.AddComponent<ShowHand>();
        showHand.isEnemy=!home;
        if (!isDummy&& IsClient &&!_subscribed)  { 
            _subscribed = true;
            hand.OnChange += showHand.OnHandChanged;
            
            
                print("We are adding board manager events  isthis server " + IsServer +"isClient : "+IsClient );
                bool allyPlayer = manager.isAlly(this);
                manager.boardAlly.OnChange += allyPlayer ? boardManager.OnBoardChangeHome : boardManager.OnBoardChangeEnemy;
                manager.boardEnemy.OnChange += allyPlayer ?   boardManager.OnBoardChangeEnemy:boardManager.OnBoardChangeHome;
            
        }

        isEnemy = !home;
        hero.Init(player.hero);
       
        state = player;


    }
    int maxMana;
    int currentMana;
    public void SpendMana(int amount)
    {
        currentMana -= amount;
        if(currentMana < 0) currentMana = 0;
        if(!isEnemy)
        ManaCrystalUI.instance.setManaCrystal(currentMana, maxMana);
    }
    void setMana(int maxMana)
    {
        
        this.maxMana = maxMana;
        currentMana = maxMana;
        if(!isEnemy)
        ManaCrystalUI.instance.setManaCrystal(maxMana, maxMana);
    }
    // Update is called once per frame
    
   
    
    public void StartEffect(ILiveTarget doer,Effect e)
    {
        List<ILiveTarget> targets =
        TargetingCenter.GetTargets(e, doer, this,manager.OtherPlayer(this));

        if (targets.Count > 0)
        {
            EndEffect(doer, targets, e);
        }
    }
    public void EndEffect(ILiveTarget doer,List<ILiveTarget> targets, Effect e)
    {
        EffectRunner.Run(e,doer,targets,this,manager.OtherPlayer(this));
        
    }
    public void Die()
    {
        Debug.Log("Meghalt a player");
        manager.GameOver(isEnemy);
    }
    int maxboardCount = 8;

    [ServerRpc]
    public void CmdPlayMinion(CardState card) // vagy int cardIndex
    {
        if (!IsServer) return;

        // 1. Ellenõrzés
       /* if (!CanPlayCard(card))
        {
            Debug.LogWarning("Card play rejected by server.");
            return;
        }*/

        // 2. LiveMinion létrehozása
        GameObject go = Instantiate(manager.minionPrefab);
        LiveMinion live = go.GetComponent<LiveMinion>();
        live.InitFromCardState(card); // beállítja a statokat a CardState alapján

        // 3. Spawn szerveren
        //ServerManager.Spawn(go);

        // 4. MinionState létrehozás és hozzáadás a boardhoz
        MinionState state = live.ToMinionState();
        //checkif its enemy blala
        manager.boardAlly.Add(state);   
        //manager.getBoard(!isEnemy).Add(state);

        RemoveCardFromHand(card);
    }
    public void RemoveCardFromHand(CardState card) { 
        
        hand.Remove(card);
    }
    
    /*
    
    [ServerRpc]
    public void CmdDraw()
    {
        print("Drawing");
        // A kód csak a szerveren fut le
        if (!IsServer)
        {
            return;
        }

        // Ellenõrizzük, hogy van-e még kártya a pakliban
        if (deck.Count == 0)
        {
            Debug.LogWarning("Nincs több lap a pakliban!");
            return;
        }

        // 1. Kiveszünk egy kártyanevet a pakliból
        ushort cardName = deck[0];
        deck.RemoveAt(0);

        // 2. Létrehozzuk a fizikai GameObject-et a LiveCard prefab-ból
        // A liveCardPrefab egy LiveCard, aminek van egy GameObject-je.
        GameObject cardGameObject = Instantiate(liveCardPrefab);
        // 3. Lekérjük a LiveCard komponenst a GameObject-rõl
        LiveCard liveCardInstance = cardGameObject.GetComponent<LiveCard>();

        // 4. Megkeressük a Card ScriptableObject-et a neve alapján
        Card cardData = GameManager.instance.GetCardById(cardName);
        
        // 5. Inicializáljuk a LiveCard objektumot a Card adatokkal
        liveCardInstance.Init(cardData);

        // 6. Spawnoljuk az objektumot a hálózaton
        // A Spawn metódus a GameObject NetworkObject komponensét várja
        base.Spawn(cardGameObject, Owner);
        liveCardInstance.transform.parent = NetworkCards_Hand;
        // 7. Hozzáadjuk a LiveCard-ot a "hand" SyncList-hez
        // Ez egy SyncList<LiveCard>, ezért a liveCardInstance-t adjuk hozzá
       // hand.Add(liveCardInstance);
    }*/
    [ServerRpc]
    public void CmdDraw()
    {
        // A kód csak a szerveren fut le
        if (!IsServer)
        {
            return;
        }

        // A pakli keverése csak a szerveren történik
        if (_serverDeckData.Count == 0)
        {
            Debug.LogWarning("Nincs több lap a pakliban!");
            return;
        }

        // 1. Kiveszünk egy kártya ID-t a pakliból

        print("huzunk");
        // 2. Létrehozunk egy CardState-et a kártya ID-vel
        CardState cardState = _serverDeckData[0];
        _serverDeckData.RemoveAt(0);
        // 3. Hozzáadjuk a CardState-et a "hand" SyncList-hez
        // Ez a FishNet-en keresztül automatikusan szinkronizálódik a klienssel
        hand.Add(cardState);

        // Itt nincs szükség Instantiate-re, GameObject-re vagy Spawn-ra!
    }

    public string sequenceIdTostring()
    {
        return"";
    }
    Arrow3DPointer arrow;
    public void StartSelectPhase(ILiveTarget doer, List<ILiveTarget> targets )
    {
        AllTargetUnvalid();
        foreach (ILiveTarget target in targets)
        {
            target.valid = true;
        }
        manager.StartSelectPhase(doer);

    }
    public void AttackWith(LiveMinion minion)
    {
        
        
        List<ILiveTarget> ls=manager.WhoIsThereToAttack(!isEnemy);
        print(" target validation "+ls.Count);
        foreach (ILiveTarget target in ls)
        {
            target.valid = true;
        }
        manager.AttackWith((ILiveTarget)minion, minion.gameObject.transform.position);
    }
    public void StartAttack(LiveMinion attackedMinion)
    {
      //  GameManager.instance.StartAttack(attackedMinion);
        AllTargetUnvalid();
        
    }

    public void AllTargetUnvalid()
    {
        Arrow3DPointer.instance.TurnOff();
        foreach (ILiveTarget target in manager.GetAlly(!isEnemy).Concat(manager.GetEnemyBoard(!isEnemy)))
        {
            target.valid = false;
        }
        // Here two hero 
    }
    #region deck methods
    static ushort _nextSequenceId = 0;
    public void CreateDeck(Deck d)
    {
        // Csak a szerveren futhat
        if (!IsServer) return;
        


        // Töröljük a korábbi listát a biztonság kedvéért
        deck.Clear();

        // Végigmegyünk a kapott paklin
        foreach (var card in d.deck)
        {
            if (card != null)
            {
                // Létrehozzuk a CardState-et az egyedi azonosítóval
                CardState newCard = new CardState
                {
                    cardId = card.cardId,
                    sequenceId = _nextSequenceId++,
                    currentCost = card.Cost
                };

                // Hozzáadjuk a CardState-et a hálózati listához
                // A FishNet automatikusan szinkronizálja az összes adatot a klienssel.
                _serverDeckData.Add(newCard);
            }
        }
    }
    [ServerRpc(RequireOwnership = true)]
    private void CmdShuffleDeck()
    {
        if (!IsServer) return;

        // Fisher-Yates shuffle algoritmus a SyncList-en
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
           /* string temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;*/
        }

        Debug.Log("Pakli megkeverve.");
    }
    #endregion

    #region Szerveres dolgok
    // Ez a metódus a szerveren fut le, és a GameManager hívja meg!
    // Nincs ServerRpc attribútum, mert a hívás a szerveren belül történik.
    
    public void ServerDraw()
    {
        ushort cardName = deck[0];
        deck.RemoveAt(0);

        // 2. Létrehozzuk a fizikai GameObject-et a LiveCard prefab-ból
        // A liveCardPrefab egy LiveCard, aminek van egy GameObject-je.
        GameObject cardGameObject = Instantiate(liveCardPrefab);
        // 3. Lekérjük a LiveCard komponenst a GameObject-rõl
        LiveCard liveCardInstance = cardGameObject.GetComponent<LiveCard>();

        // 4. Megkeressük a Card ScriptableObject-et a neve alapján
        Card cardData = GameManager.instance.GetCardById(cardName);

        // 5. Inicializáljuk a LiveCard objektumot a Card adatokkal
        liveCardInstance.Init(cardData);

        // 6. Spawnoljuk az objektumot a hálózaton
        // A Spawn metódus a GameObject NetworkObject komponensét várja
        base.Spawn(cardGameObject, Owner);
        liveCardInstance.transform.parent=NetworkCards_Hand;
        
        // 7. Hozzáadjuk a LiveCard-ot a "hand" SyncList-hez
        // Ez egy SyncList<LiveCard>, ezért a liveCardInstance-t adjuk hozzá
       // hand.Add(liveCardInstance);
        // Ez a kód csak a szerveren fut le.
        print("Szerver: Kártyahúzás Player2 számára.");
        // Ide jön a kártyahúzás valós logikája Player2-nek.
    }
    public GameObject gameController;
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Kliens oldalon megkeressük a GameManager-t
        CreateGameController();
        
        
        
        manager=GameObject.Find("GameController").GetComponent<GameManager>();
        bool ally = manager.isAlly(this);
        Debug.Log($"Subscribed OnChange. IsServer:{IsServer} IsClient:{IsClient}ally?{ally}");
        if (IsOwner && manager != null)
        {
            print("Csatlakozunk és jo minden");
            // Ha ez a helyi klienshez tartozik, akkor szólunk a szervernek
            // hogy regisztráljon minket a GameManager-ben
            manager.RegisterPlayer(this);
        }
    }
    public void  CreateGameController()
    {
       // base.Spawn(gameController);
    }
    public override void OnStopClient()
    {
        bool ally = manager.isAlly(this);
        manager.boardAlly.OnChange -= ally
            ? boardManager.OnBoardChangeHome
            : boardManager.OnBoardChangeEnemy;

        manager.boardEnemy.OnChange -= ally
            ? boardManager.OnBoardChangeEnemy
            : boardManager.OnBoardChangeHome;
    }
    #endregion

}
