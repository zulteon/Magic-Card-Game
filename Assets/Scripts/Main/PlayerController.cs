using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Timing;
using Unity.VisualScripting;
using System;
using FishNet.Connection;

public class PlayerController : NetworkBehaviour
{
    // A SyncList automatikusan szinkronizálja a szerveroldali változásokat a kliensekkel.
    private readonly List<CardState> _deck = new();
    [SerializeReference]
    public readonly SyncList<CardState> hand = new ();// saját oldalon lévő minionok
    //new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.OwnerOnly));
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
       NetworkCards_Hand = new GameObject("HandNetworkCardData").transform;
       NetworkCards_Hand.transform.parent = transform;
       NetworkCards_Board = new GameObject("BoardNetworkCardData").transform;
       NetworkCards_Board.transform.parent = transform;
        Debug.Log(typeof(string).Assembly.ImageRuntimeVersion);
    }
    void Update()
    {
        if (!IsOwner) return;
        if (GameManager.instance == null || GameManager.instance.offlineTestMode) return;

        if (Input.GetKeyDown(KeyCode.D)) RequestDrawServerRpc();
        if (Input.GetKeyDown(KeyCode.F)) RequestPlayFirstServerRpc();
        if (Input.GetKeyDown(KeyCode.Return)) RequestEndTurnServerRpc();
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestDrawServerRpc() => DrawCard();

    [ServerRpc(RequireOwnership = true)]
    private void RequestPlayFirstServerRpc()
    {
        if (hand.Count == 0) return;
        PlayMinion(hand[0]);
    }

    [ServerRpc(RequireOwnership = true)]
    private void RequestEndTurnServerRpc() => GameManager.instance.EndTurn();
    public GameObject liveCardPrefab;
    public LiveHero hero { get; set; }
    int Health;
    public ShowHand showHand;
    public GameManager manager;
    public readonly SyncVar<bool> isEnemy = new();
    public PlayerState state;

    private bool _subscribed = false;
    public ushort heroId;
    [Server]
    public void Init(PlayerState player, bool home, GameManager manager2)
    {
        manager = GameManager.instance;
        boardManager = manager.gameObject.GetComponent<BoardManager>();
        liveCardPrefab = manager.liveCardPrefab;

        isEnemy.Value = !home;                      // EGYSZER, nem háromszor
        heroId = isEnemy.Value ? (ushort)1 : (ushort)0;

        CreateDeck(player.deck);              // <- az ID-allokátorhoz kell a home

        hero = gameObject.GetComponent<LiveHero>();
        if (hero == null) hero = gameObject.AddComponent<LiveHero>();
        hero.Init(player.hero);

        state = player;
    }
    int maxMana;
    public readonly SyncVar<int> currentResource=new(0);
    public PlayerEconomy economy;
    /*
    public void SpendMana(int amount)
    {
        currentMana -= amount;
        if(currentMana < 0) currentMana = 0;
        if(!isEnemy.Value)
        ManaCrystalUI.instance.setManaCrystal(currentMana, maxMana);
    }
    void setMana(int maxMana)
    {
        
        this.maxMana = maxMana;
        currentMana = maxMana;
        if(!isEnemy.Value)
        ManaCrystalUI.instance.setManaCrystal(maxMana, maxMana);
    }*/
    public void Die()
    {
        Debug.Log("Meghalt a player");
        manager.GameOver(isEnemy.Value);
    }
    int maxboardCount = 8;
    static ushort minionSequenceId=1;//0,1 heros main
    [Server]
    public void PlayMinion(CardState card)
    {
        GameObject go = Instantiate(manager.minionPrefab);
        LiveMinion live = go.GetComponent<LiveMinion>();

        //live.InitFromCardState(card); // beállítja a statokat a CardState alapján

        minionSequenceId = manager.NextCardId(!isEnemy.Value);

        // 4. MinionState létrehozás és hozzáadás a boardhoz
        MinionState state = MinionStateFactory.FromCardState(card, minionSequenceId);
        //print("thi is my state " + state.ToString() + state.cardId + state.GetType());
        MinionLogic minionLogic=manager.CreateMinionLogic(minionSequenceId);
        if (!isEnemy.Value)
            manager.boardAlly.Add(state);
        else
            manager.boardEnemy.Add(state);
        RemoveCardFromHand(card);

        // --- BATTLECRY CHECK HELYE ---
        // Lekérjük a statikus adatokat (ScriptableObject), hogy lássuk az effekteket
        MinionCard data = CardManager.instance.GetMinion(card.cardId);

        // print(data.effectIds[0]);
        Effect[] battlecry = TriggerChecker.instance.CheckTrigger(Trigger.time.instant, data).ToArray();
        //effects.Remove(battlecry)
        manager.DoEffects(battlecry, minionSequenceId, this);
        
        manager.RegisterAbilities(minionLogic.effectBag,minionSequenceId, EffectManagerClient.instance.GetEffectData(data.effectIds).ToArray(),Zone.Board);
       /* MinionLogic logic = manager.GetMinionLogic(minionSequenceId);
        List<Effect> effects =
            EffectManagerClient.instance.GetEffectData(data.effectIds);
        List<Trigger.time> activeTriggeringEffects = new List<Trigger.time>() { Trigger.time.startofturn, Trigger.time.endofturn };
        foreach (Effect effect in effects)
        {
            if (effect == null || effect.triggers == null || effect.triggers.Length == 0)
                continue;
            Trigger.time when = effect.triggers[0].t;
           
                
            if( TriggerConverter.ActiveEffectConverter(when, out GameEvents.EventType gameEvent))
            {
                ushort capturedId = minionSequenceId;
                print("effect added " +effect.type.ToString() +" to game events "+ gameEvent.ToString() + "doer " +minionSequenceId.ToString() );
                GameEvents.Instance.AddEvent(minionSequenceId, gameEvent, () => EffectRunner.Run(effect, capturedId));
            } 
        }*/
    }
    // ott, ahol most a feliratkozó foreach van (szerveroldal)
    
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
    
    
    [Server]
    public void DrawCard()
    {
        if (_deck.Count == 0)
        {
            Debug.LogWarning("Nincs több lap a pakliban!");
            return;   // TODO: fatigue
        }
        // todo ha a kéz 10 kártya van  a lép  ég 
        CardState cs = _deck[0];
        _deck.RemoveAt(0);
        hand.Add(cs);                    // <- előbb a listába

        GameManager.instance.MoveCard(cs.sequenceId, Zone.Deck, Zone.Hand);
       // GameEvents.Instance.RaiseCardDrawn(cs.cardId);
    }
    Arrow3DPointer arrow;

   
    public LiveMinion attacker;
    public void StartAttack(LiveMinion attacker)
    {
        GameManager.instance.phase = GameManager.Phase.targeting;
        this.attacker= attacker;
        SelectTarget.instance.Ready(true);
    }
    public void EndAttack(LiveMinion victim)
    {
        GameManager.instance.ExecuteAttack(attacker.sequenceId, victim.sequenceId);
    }

    public void AllTargetUnvalid()
    {
        Arrow3DPointer.instance.TurnOff();
        /*foreach (ILiveTarget target in manager.GetAlly(!isEnemy.Value).Concat(manager.GetEnemyBoard(!isEnemy.Value)))
        {
            target.valid = false;
        }*/
        // Here two hero 
    }
    #region deck methods ---------------->
    static ushort _nextSequenceId = 0;
    [Server]
    public void CreateDeck(Deck d)
    {
        _deck.Clear();

        foreach (var card in d.deck)
        {
            if (card == null) continue;

            _deck.Add(new CardState
            {
                cardId = card.cardId,
                sequenceId = GameManager.instance.NextCardId(!isEnemy.Value),
                currentCost = card.Cost
            });
        }

        //Shuffle();
    }

    [Server]
    private void Shuffle()
    {
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }
    public bool TryFindCard(ushort seqId, out Zone zone, out int index)
    {
        for (int i = 0; i < hand.Count; i++)
            if (hand[i].sequenceId == seqId) { zone = Zone.Hand; index = i; return true; }

        for (int i = 0; i < _deck.Count; i++)
            if (_deck[i].sequenceId == seqId) { zone = Zone.Deck; index = i; return true; }

        zone = Zone.None; index = -1;
        return false;
    }

    public CardState GetCard(Zone zone, int index)
        => zone == Zone.Hand ? hand[index] : _deck[index];

    public void SetCard(Zone zone, int index, CardState cs)
    {
        if (zone == Zone.Hand) hand[index] = cs;    // force:true → megy a hálóra
        else _deck[index] = cs;   // szerveroldali, nincs szinkron
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

        showHand = gameObject.GetComponent<ShowHand>();
        showHand.isEnemy = !IsOwner;
        hand.OnChange += showHand.OnHandChanged;
        economy = new PlayerEconomy(this);
        manager = GameManager.instance;
        boardManager = manager.gameObject.GetComponent<BoardManager>();

        if (!IsOwner) return;

        RegisterMeServerRpc();          

        isEnemy.OnChange += OnRoleReceived;

    }
    // PlayerController
    [TargetRpc]
    public void RoleAssignedTargetRpc(NetworkConnection conn, bool enemy) => SubscribeBoards(enemy);
    private void OnRoleReceived(bool prev, bool next, bool asServer)
    {
        if (asServer) return;
        SubscribeBoards(next);
    }

    private bool _boardsSubscribed;

    private void SubscribeBoards(bool enemy)
    {
        if (_boardsSubscribed) return;
        _boardsSubscribed = true;

        bool ally = !enemy;

        boardManager.SetSide(ally);// ami nekem "enemy"

        manager.boardAlly.OnChange += ally ? boardManager.OnBoardChangeHome : boardManager.OnBoardChangeEnemy;
        manager.boardEnemy.OnChange += ally ? boardManager.OnBoardChangeEnemy : boardManager.OnBoardChangeHome;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (showHand != null) hand.OnChange -= showHand.OnHandChanged;

        isEnemy.OnChange -= OnRoleReceived;

        if (!_boardsSubscribed) return;
        _boardsSubscribed = false;

        if (manager == null || boardManager == null) return;

        bool ally = !isEnemy.Value;
        manager.boardAlly.OnChange -= ally ? boardManager.OnBoardChangeHome : boardManager.OnBoardChangeEnemy;
        manager.boardEnemy.OnChange -= ally ? boardManager.OnBoardChangeEnemy : boardManager.OnBoardChangeHome;
    }
    [ServerRpc(RequireOwnership = true)]
    private void RegisterMeServerRpc()
    {
        GameManager.instance.RegisterPlayer(this);

    }
    #endregion
    #region Debug

    void printOutHand()
    {
        foreach (var i in hand)
        {
            print(i.cardId);
        }
    }
    #endregion
}
