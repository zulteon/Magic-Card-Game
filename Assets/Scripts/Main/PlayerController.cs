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
    // PlayerController
    // PlayerController
    public readonly SyncVar<MinionState> heroState = new();
    public MinionLogic heroLogic;                          // szerveroldali, NEM a minionLogics-ban

    [Server]
    public void InitHero(int startingHealth)
    {
        ushort heroId = isEnemy.Value ? (ushort)1 : (ushort)0;

        heroState.Value = new MinionState
        {
            sequenceId = heroId,
            currentHealth = (ushort)startingHealth,
            attack = 0,
            canAttack = false,
            activeEffects = new List<ushort>()
        };

        heroLogic = new MinionLogic(heroId /* + amit a konstruktor kér */);
    }
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
                                              // GameManager, a játék indításakor
       // playerB.Value.GiveCoin();   
        hero = gameObject.GetComponent<LiveHero>();
        if (hero == null) hero = gameObject.AddComponent<LiveHero>();
        hero.Init(player.hero);

        state = player;
    }
    int maxMana;
    public readonly SyncVar<int> currentResource=new(0);
    public readonly SyncVar<int> maxResource = new(0);
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
    public void PlayMinion(CardState card,List<ushort> batlecryVictims=null)
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
        bool first = true;
        foreach (Effect effect in battlecry)
        {
            manager.DoEffect(effect, minionSequenceId, this, targets: first ? batlecryVictims : null);
            first = false;
        }

        manager.SendClientEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.summon,
            targetIds = new[] { minionSequenceId },
            newValues = new[] { isEnemy.Value ? 0 : 1 }
        });

        manager.RegisterAbilities(minionLogic.effectBag,minionSequenceId, EffectManagerClient.instance.GetEffectData(data.effectIds).ToArray(),Zone.Board);
        
        //GameEvents.Instance.RaiseMinionSummoned(minionLogic);
    }
    public void Summon(ushort summonAbleId,bool homeSummon=true)
    {
        bool toEnemyBoard = isEnemy.Value == homeSummon;
        minionSequenceId = manager.NextCardId(toEnemyBoard);
        MinionState state=MinionStateFactory.FromMinionData(summonAbleId,minionSequenceId);
        MinionLogic minionLogic = manager.CreateMinionLogic(minionSequenceId);
        
        if (!toEnemyBoard)
            manager.boardAlly.Add(state);
        else
            manager.boardEnemy.Add(state);
        manager.RegisterAbilities(minionLogic.effectBag, minionSequenceId,
    EffectManagerClient.instance.GetEffectData(state.activeEffects).ToArray(),
    Zone.Board);
      /*  manager.SendClientEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.summon,
            targetIds = new[] { minionSequenceId },
            newValues = new[] { toEnemyBoard ? 0 : 1 }
        });*/
        GameEvents.Instance.RaiseMinionSummoned(minionLogic);
    }
    // ott, ahol most a feliratkozó foreach van (szerveroldal)

    [ServerRpc(RequireOwnership = true)]
    public void CmdPlayMinion(CardState card, ushort victimId) // vagy int cardIndex
    {
        // ellenörzés : IsVictimValid? if(victimId==ushortMaxValue)
        
        print("playing a minion " + card.ToString());
        PlayMinion(card,victimId==ushort.MaxValue?null:new List<ushort> { victimId});


        // 1. Ellenőrzés
        /* if (!CanPlayCard(card))
         {
             Debug.LogWarning("Card play rejected by server.");
             return;
         }*/
    }
    #region SelectTarget OnClient


    [Client]
    public void BeforePlay(CardState card)
    {
        var def = CardManager.instance.GetCard(card.cardId);
        if (def == null) return;

        bool isSpell = def.GetCardType() == CardType.Spell;

        // Board-limit csak lényre vonatkozik
        if (!isSpell)
        {
            var board = isEnemy.Value ? manager.boardEnemy : manager.boardAlly;
            if (board.Count >= GameManager.BOARD_LIMIT) { Debug.Log("Tele a pálya"); return; }
        }

        Effect[] e = TriggerChecker.instance.CheckTrigger(Trigger.time.instant, def).ToArray();
        Effect needsTarget = FindTargetedEffect(e);

        if (needsTarget == null || needsTarget.target == Trigger.Target.self) { SendPlay(card, ushort.MaxValue, isSpell); return; }
        _validTargetsToSelect = TargetingCenter.GetTargets(needsTarget, card.sequenceId, this);

        if (_validTargetsToSelect == null || _validTargetsToSelect.Count == 0)
        {
            if (isSpell)
            {
                Debug.Log("Nincs érvényes célpont, a lap marad a kézben.");
                return;
            }

            SendPlay(card, ushort.MaxValue, false);   // lény: kijön, battlecry nélkül
            return;
        }
        TargetSelector.instance.Begin(
            _validTargetsToSelect,
            new Vector3(0f, -3.0f, 0f),
            targetId => SendPlay(card, targetId, isSpell));
    }

    private void SendPlay(CardState card, ushort targetId, bool isSpell)
    {
        print("SENDING INTO PLAY "+card.sequenceId.ToString());
        if (isSpell) CmdPlaySpell(card, targetId);
        else CmdPlayMinion(card, targetId);
    }
    [ServerRpc(RequireOwnership = true)]
    public void CmdPlaySpell(CardState card, ushort victimId)
    {
        //if (!GameManager.instance.IsPlayersTurn(this)) return;

        var def = CardManager.instance.GetCard(card.cardId);
        if (def == null || def.GetCardType() != CardType.Spell) return;

        PlaySpell(card, victimId == ushort.MaxValue ? null : new List<ushort> { victimId });
    }

    [Server]
    private void PlaySpell(CardState card, List<ushort> victims = null)
    {
        var def = CardManager.instance.GetCard(card.cardId);
        Effect[] effects = EffectManagerClient.instance.GetEffectData(def.effectIds).ToArray();

        // Célzott spell célpont nélkül nem játszható ki
        Effect needsTarget = FindTargetedEffect(effects);
        if (needsTarget != null && (victims == null || victims.Count == 0))
        {
            Debug.LogWarning("Célzott spell célpont nélkül — elutasítva.");
            return;
        }

        // Mana CSAK az ellenőrzések után
        if (!economy.TrySpendResource(def.cost)) { Debug.Log("Nincs elég mana"); return; }

        RemoveCardFromHand(card);
        GameManager.instance.cardBags.Remove(card.sequenceId, RemoveReason.ZoneChange);

        bool first = true;
        foreach (Effect effect in effects)
        {
            manager.DoEffect(effect, heroId, this, targets: first ? victims : null);
            first = false;
        }

        manager.graveyard.DeadCards.Add(card.cardId);
        manager.graveyard.Execute();
    }

    /// <summary>Az első olyan instant effekt, ami kézi célzást igényel.</summary>
    private static Effect FindTargetedEffect(Effect[] effects)
    {
        foreach (var e in effects)
        {
            if (e != null && e.targetCast == Effect.TargetCast.single && !e.random) // multitargethez expandálni kell a mechanikát
                return e;
            return null; // mindig az első effekt ami selectiont igényel
        }

        return null;
    }

    // ── állapot ──
    private List<ushort> _validTargetsToSelect;

    
    #endregion
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
        // Kéz-limit: a lap megsemmisül, NEM kerül a gyűjtőbe
        if (hand.Count >= HAND_LIMIT)
        {
            GameManager.instance.SendClientEvent(new ClientEvent
            {
                effectType = (ushort)Effect.Type.cardDestroyed,
                targetIds = new ushort[] { cs.sequenceId },
                value = cs.cardId,      // hogy a kliens tudja, MIT mutasson
                doerId = cs.sequenceId
            });

            return;   // se hand.Add, se MoveCard — a lap eltűnik
        }


        hand.Add(cs);                    // <- előbb a listába

        GameManager.instance.MoveCard(cs.sequenceId, Zone.Deck, Zone.Hand);
       // GameEvents.Instance.RaiseCardDrawn(cs.cardId);
    }
    private const int HAND_LIMIT = 10;
    Arrow3DPointer arrow;

   
    public void StartAttack(LiveMinion attacker)
    {
        ushort attackerId = attacker.sequenceId;

        TargetSelector.instance.Begin(
            GetValidAttackTargets(),
            attacker.transform.position,
            victimId => CmdAttack(attackerId, victimId));
    }

    private List<ushort> GetValidAttackTargets()
    {
        var all = new List<ushort>();
        var taunts = new List<ushort>();

        foreach (var m in manager.GetEnemyBoard(!isEnemy.Value))
        {
            all.Add(m.sequenceId);
            if (m.taunt) taunts.Add(m.sequenceId);
        }

        // Ha van taunt, csak azok támadhatók
        if (taunts.Count > 0) return taunts;

        // Különben minden lény + az ellenséges hős
        all.Add(manager.GetHeroId(this));
        return all;
    }
    [ServerRpc(RequireOwnership = true)]
    private void CmdAttack(ushort attackerId, ushort victimId)
    {
        GameManager.instance.ExecuteAttack(attackerId, victimId);
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
        isEnemy.OnChange += OnRoleReceived;
        RegisterMeServerRpc();          

        

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
