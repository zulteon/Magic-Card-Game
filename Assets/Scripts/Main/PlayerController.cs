using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Timing;
using Unity.VisualScripting;
using System;

public class PlayerController : NetworkBehaviour
{
    // A SyncList automatikusan szinkronizálja a szerveroldali változásokat a kliensekkel.
    public readonly SyncList<ushort> deck = new ();
    [SerializeReference]
    public readonly SyncList<CardState> hand = new ();// saját oldalon lévő minionok
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
        Debug.Log(typeof(string).Assembly.ImageRuntimeVersion);
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
    public ushort heroId;
    public void Init(PlayerState player,bool home,GameManager manager2)
    {
        // Probably this method not needed we leave it for now
        if (manager == null)
            manager = manager2;
        boardManager=manager.gameObject.GetComponent<BoardManager>();
        //Debug.Log("This is enemy: " + isEnemy + " is this player a?" + manager.isAlly(this) + "are we client? " + IsClient+"Is this haho");
        liveCardPrefab = manager.liveCardPrefab;
        if(IsServer)
        CreateDeck(player.deck);
        
        hero =gameObject.GetComponent<LiveHero>();
        
        if (hero==null)
            hero =gameObject.AddComponent<LiveHero>();
        if(showHand==null)
            showHand=gameObject.AddComponent<ShowHand>();
        showHand.isEnemy=!home;
        heroId = isEnemy ?(ushort) 1 :(ushort) 0;
        if (!_subscribed&&IsClient)  { 
            _subscribed = true;
            
            
            
                
            
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
    
   
    
   /* public void StartEffect(ILiveTarget doer,Effect e)
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
        
    }*/
    public void Die()
    {
        Debug.Log("Meghalt a player");
        manager.GameOver(isEnemy);
    }
    int maxboardCount = 8;
    static ushort minionSequenceId=0;
    [Server]
    public void PlayMinion(CardState card)
    {
        GameObject go = Instantiate(manager.minionPrefab);
        LiveMinion live = go.GetComponent<LiveMinion>();

        //live.InitFromCardState(card); // beállítja a statokat a CardState alapján


        // 4. MinionState létrehozás és hozzáadás a boardhoz
        minionSequenceId++;
        MinionState state = MinionStateFactory.FromCardState(card, minionSequenceId);
        print("thi is my state " + state.ToString() + state.cardId + state.GetType());
        if(!isEnemy) 
            manager.boardAlly.Add(state);
        else 
            manager.boardEnemy.Add(state);
        RemoveCardFromHand(card);
    }
    [ServerRpc]
    public void CmdPlayMinion(CardState card) // vagy int cardIndex
    {
        if (!IsServer) return;

        // 1. Ellenőrzés
        /* if (!CanPlayCard(card))
         {
             Debug.LogWarning("Card play rejected by server.");
             return;
         }*/
        print("playing a minion " + card.ToString());
        PlayMinion(card);
        

        
    }
    public void RemoveCardFromHand(CardState card) { 
        
        hand.Remove(card);
    }
    
    
    [ServerRpc]
    public void CmdDraw()
    {
        DrawCard();
    }
    [Server]
    public void DrawCard()
    {
        // A kártyahúzás teljes logikája
        if (_serverDeckData.Count == 0)
        {
            Debug.LogWarning("Nincs több lap a pakliban!");
            return;
        }
        print("huzas");
        CardState cardState = _serverDeckData[0];
        _serverDeckData.RemoveAt(0);
        hand.Add(cardState);
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
   
    public LiveMinion attacker;
    public void StartAttack(LiveMinion attacker)
    {
        GameManager.instance.phase = GameManager.Phase.targeting;
        this.attacker= attacker;
        SelectTarget.instance.Ready(true);
        //  GameManager.instance.StartAttack(attackedMinion);
        //AllTargetUnvalid();
        //CombatHandler.instance.AttackIt(attacker, attackedMinion);
        //manager.Damage(attacker, attackedMinion);
    }
    public void EndAttack(LiveMinion attackedMinion)
    {
        print("endattack"+attackedMinion+attacker); 
        CombatHandler.instance.Attack(attacker, attackedMinion);
        //manager.DamageServerRpc(attacker, attackedMinion);
        print("target pos" + attacker.transform.position + " victim pos" + attackedMinion.transform.position);
    }

    public void AllTargetUnvalid()
    {
        Arrow3DPointer.instance.TurnOff();
        /*foreach (ILiveTarget target in manager.GetAlly(!isEnemy).Concat(manager.GetEnemyBoard(!isEnemy)))
        {
            target.valid = false;
        }*/
        // Here two hero 
    }
    #region deck methods ---------------->
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
           // int j = Random.Range(0, i + 1);
           /* string temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;*/
        }

        Debug.Log("Pakli megkeverve.");
    }
    #endregion

    #region Szerveres dolgok  ---------------->


    public GameObject gameController;
    public override void OnStartServer()
    {
        base.OnStartServer();
            
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Kliens oldalon megkeressük a GameManager-t
        showHand = gameObject.GetComponent<ShowHand>(); 
        hand.OnChange += showHand.OnHandChanged;

        manager =GameObject.Find("GameController").GetComponent<GameManager>();
        boardManager = manager.gameObject.GetComponent<BoardManager>();
        print("Manager is nullll +" + manager == null);
        if (IsOwner && manager != null)
        {
            print("Csatlakozunk és jo minden");
            // Ha ez a helyi klienshez tartozik, akkor szólunk a szervernek
            // hogy regisztráljon minket a GameManager-ben
            manager.RegisterPlayer(this);
        
            bool allyPlayer = manager.isAlly(this);
            print($"iss ally player {allyPlayer}");
            manager.boardAlly.OnChange += allyPlayer ? boardManager.OnBoardChangeHome : boardManager.OnBoardChangeEnemy;
            manager.boardEnemy.OnChange += allyPlayer ? boardManager.OnBoardChangeEnemy : boardManager.OnBoardChangeHome;
            // Itt kell csekkolni hogy ez már a második player e egyelőre 1 player van
           // if (IsOwner)
                //StartGameServerRpc();
        }
        bool ally = manager.isAlly(this);
        
        Debug.Log($"Subscribed OnChange. IsServer:{IsServer} IsClient:{IsClient} ally?{ally} dummy?{isDummy}");


        
        
        
    }
    [ServerRpc]
    private void StartGameServerRpc()
    {
        // A játék indításának logikája, ami a szerveren fut le.
        // Innen hívhatod meg a GameManagert.
       // manager.StartGame();
    }
    public void  CreateGameController()
    {
        base.Spawn(gameController);
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
