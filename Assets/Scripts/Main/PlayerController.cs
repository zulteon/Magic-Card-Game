using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Timing;

public class PlayerController : NetworkBehaviour
{
    // A SyncList automatikusan szinkroniz�lja a szerveroldali v�ltoz�sokat a kliensekkel.
    public readonly SyncList<ushort> deck = new ();
    public readonly SyncList<CardState> hand = new ();// saj�t oldalon l�v� minionok
    private readonly List<CardState> _serverDeckData = new();
    // Itt t�roljuk a k�rty�k aktu�lis �llapot�t a pakliban
    //private Dictionary<string, CardState> cardStates = new Dictionary<string, CardState>();
    /*Ez a LiveCard (dinamikus �llapot) �s a Card (statikus adat) k�z�tti megk�l�nb�ztet�s kulcsfontoss�g�.
     * A SyncList-ednek valahogy a buffokat is t�rolnia kell, ha a k�rty�k visszaker�lnek a pakliba.
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

        // 1. Ellen�rz�s
       /* if (!CanPlayCard(card))
        {
            Debug.LogWarning("Card play rejected by server.");
            return;
        }*/

        // 2. LiveMinion l�trehoz�sa
        GameObject go = Instantiate(manager.minionPrefab);
        LiveMinion live = go.GetComponent<LiveMinion>();
        live.InitFromCardState(card); // be�ll�tja a statokat a CardState alapj�n

        // 3. Spawn szerveren
        //ServerManager.Spawn(go);

        // 4. MinionState l�trehoz�s �s hozz�ad�s a boardhoz
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
        // A k�d csak a szerveren fut le
        if (!IsServer)
        {
            return;
        }

        // Ellen�rizz�k, hogy van-e m�g k�rtya a pakliban
        if (deck.Count == 0)
        {
            Debug.LogWarning("Nincs t�bb lap a pakliban!");
            return;
        }

        // 1. Kivesz�nk egy k�rtyanevet a paklib�l
        ushort cardName = deck[0];
        deck.RemoveAt(0);

        // 2. L�trehozzuk a fizikai GameObject-et a LiveCard prefab-b�l
        // A liveCardPrefab egy LiveCard, aminek van egy GameObject-je.
        GameObject cardGameObject = Instantiate(liveCardPrefab);
        // 3. Lek�rj�k a LiveCard komponenst a GameObject-r�l
        LiveCard liveCardInstance = cardGameObject.GetComponent<LiveCard>();

        // 4. Megkeress�k a Card ScriptableObject-et a neve alapj�n
        Card cardData = GameManager.instance.GetCardById(cardName);
        
        // 5. Inicializ�ljuk a LiveCard objektumot a Card adatokkal
        liveCardInstance.Init(cardData);

        // 6. Spawnoljuk az objektumot a h�l�zaton
        // A Spawn met�dus a GameObject NetworkObject komponens�t v�rja
        base.Spawn(cardGameObject, Owner);
        liveCardInstance.transform.parent = NetworkCards_Hand;
        // 7. Hozz�adjuk a LiveCard-ot a "hand" SyncList-hez
        // Ez egy SyncList<LiveCard>, ez�rt a liveCardInstance-t adjuk hozz�
       // hand.Add(liveCardInstance);
    }*/
    [ServerRpc]
    public void CmdDraw()
    {
        // A k�d csak a szerveren fut le
        if (!IsServer)
        {
            return;
        }

        // A pakli kever�se csak a szerveren t�rt�nik
        if (_serverDeckData.Count == 0)
        {
            Debug.LogWarning("Nincs t�bb lap a pakliban!");
            return;
        }

        // 1. Kivesz�nk egy k�rtya ID-t a paklib�l

        print("huzunk");
        // 2. L�trehozunk egy CardState-et a k�rtya ID-vel
        CardState cardState = _serverDeckData[0];
        _serverDeckData.RemoveAt(0);
        // 3. Hozz�adjuk a CardState-et a "hand" SyncList-hez
        // Ez a FishNet-en kereszt�l automatikusan szinkroniz�l�dik a klienssel
        hand.Add(cardState);

        // Itt nincs sz�ks�g Instantiate-re, GameObject-re vagy Spawn-ra!
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
        


        // T�r�lj�k a kor�bbi list�t a biztons�g kedv��rt
        deck.Clear();

        // V�gigmegy�nk a kapott paklin
        foreach (var card in d.deck)
        {
            if (card != null)
            {
                // L�trehozzuk a CardState-et az egyedi azonos�t�val
                CardState newCard = new CardState
                {
                    cardId = card.cardId,
                    sequenceId = _nextSequenceId++,
                    currentCost = card.Cost
                };

                // Hozz�adjuk a CardState-et a h�l�zati list�hoz
                // A FishNet automatikusan szinkroniz�lja az �sszes adatot a klienssel.
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
    // Ez a met�dus a szerveren fut le, �s a GameManager h�vja meg!
    // Nincs ServerRpc attrib�tum, mert a h�v�s a szerveren bel�l t�rt�nik.
    
    public void ServerDraw()
    {
        ushort cardName = deck[0];
        deck.RemoveAt(0);

        // 2. L�trehozzuk a fizikai GameObject-et a LiveCard prefab-b�l
        // A liveCardPrefab egy LiveCard, aminek van egy GameObject-je.
        GameObject cardGameObject = Instantiate(liveCardPrefab);
        // 3. Lek�rj�k a LiveCard komponenst a GameObject-r�l
        LiveCard liveCardInstance = cardGameObject.GetComponent<LiveCard>();

        // 4. Megkeress�k a Card ScriptableObject-et a neve alapj�n
        Card cardData = GameManager.instance.GetCardById(cardName);

        // 5. Inicializ�ljuk a LiveCard objektumot a Card adatokkal
        liveCardInstance.Init(cardData);

        // 6. Spawnoljuk az objektumot a h�l�zaton
        // A Spawn met�dus a GameObject NetworkObject komponens�t v�rja
        base.Spawn(cardGameObject, Owner);
        liveCardInstance.transform.parent=NetworkCards_Hand;
        
        // 7. Hozz�adjuk a LiveCard-ot a "hand" SyncList-hez
        // Ez egy SyncList<LiveCard>, ez�rt a liveCardInstance-t adjuk hozz�
       // hand.Add(liveCardInstance);
        // Ez a k�d csak a szerveren fut le.
        print("Szerver: K�rtyah�z�s Player2 sz�m�ra.");
        // Ide j�n a k�rtyah�z�s val�s logik�ja Player2-nek.
    }
    public GameObject gameController;
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Kliens oldalon megkeress�k a GameManager-t
        CreateGameController();
        
        
        
        manager=GameObject.Find("GameController").GetComponent<GameManager>();
        bool ally = manager.isAlly(this);
        Debug.Log($"Subscribed OnChange. IsServer:{IsServer} IsClient:{IsClient}ally?{ally}");
        if (IsOwner && manager != null)
        {
            print("Csatlakozunk �s jo minden");
            // Ha ez a helyi klienshez tartozik, akkor sz�lunk a szervernek
            // hogy regisztr�ljon minket a GameManager-ben
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
