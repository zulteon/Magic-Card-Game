using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;
public class GameManager : NetworkBehaviour
{
    #region 1. SINGLETON & MEZŐK
    //public static event Action OnTurnEnd,OnTurnStart;
    public static GameManager instance;

    public static int BOARD_LIMIT = 8;
    private GameEvents gameEvents;
    GameOverHandler gameOverHandler;
    private int localPlayerIndex=0;
    public Effect TestEffect;
    public bool online = true;
    public NetworkManager networkManager;
    public readonly Graveyard graveyard = new();
    public GameObject liveCardPrefab;
    public bool offlineTestMode;
    public bool turnOff = false;
    public enum Phase { ready, animation, targeting }
    public Phase phase = Phase.ready;
    GameState gameState;

    public readonly SyncVar<PlayerController> playerA = new SyncVar<PlayerController>();
    public readonly SyncVar<PlayerController> playerB = new SyncVar<PlayerController>();
    public Deck testDeck;

    #region CardTemplate
    [Header("prefabs")]
    public GameObject cardTemplateFront;
    public GameObject cardTemplateBack;
    public GameObject minionPrefab;
    public GameObject minionUIPrefab;
    public void GetCardTemplates(out GameObject front, out GameObject back)
    {
        front = cardTemplateFront;
        back = cardTemplateBack;
    }
    #endregion

    #region Board
    public readonly SyncList<MinionState> boardAlly=new();
    public readonly SyncList<MinionState> boardEnemy=new(); 

    public bool isAllyMinion(ushort id)
    {

        if (id == 0) return true;
        if (id == 1) return false;
        for (int i = 0; i < boardAlly.Count; i++) { 
            if(boardAlly[i].sequenceId == id) return true;
        }
        for(int i = 0; i < boardEnemy.Count; i++) {
            if (boardEnemy[i].sequenceId == id) return false;
        }
        Debug.Log($"<color=red>IsAlly minion : {id} not found in synclist board</color>");
        return false;
    }

    public List<MinionState> GetAlly(bool homePerspective = true)
    {
        return homePerspective ? boardAlly.ToList() : boardEnemy.ToList();

    }
    public void AddAlly(MinionState ally)
    {
        try
        {
            boardAlly.Add(ally);
        }
        catch { }
    }
    public List<MinionState> GetEnemyBoard(bool homePerspective = true)
    {
        return !homePerspective ? boardAlly.ToList() : boardEnemy.ToList();

    }
    
    public SyncList<MinionState> getBoard(bool ally,bool homePerspective = true)
    {
        if(ally)
            return homePerspective?boardAlly:boardEnemy;
        else
            return !homePerspective?boardAlly:boardEnemy;
    }
    #endregion
    
    #region Minion
    public MinionState GetMinionById(ushort sequenceId)
    {
        if (sequenceId < 2)
            return GetPlayerByIndex(sequenceId).heroState.Value;
       foreach(var minion in boardAlly.Concat(boardEnemy))
        {
            if(minion.sequenceId == sequenceId) return  minion;
        }
        return default;
        //foreach in heros
    }
    public int GetBoardIndex(ushort id)
    {
        for (int i = 0; i < boardAlly.Count; i++)
            if (boardAlly[i].sequenceId == id) return i;

        for (int i = 0; i < boardEnemy.Count; i++)
            if (boardEnemy[i].sequenceId == id) return i;

        return -1;
    }
    public bool HasMinion(ushort sequenceId)
    {
        foreach (var minion in boardAlly.Concat(boardEnemy))
        {
            if (minion.sequenceId == sequenceId) return true;
        }
        return false;
    }
    public void ChangeMinionByIndex(int id,MinionState minion)
    {
        if (id < boardAlly.Count)
        {
            boardAlly[id] = minion;
        }
        else
        {
            boardEnemy[id-boardAlly.Count] = minion;
        }
    }
    public void SetTaunt(ushort id, bool value)
    => ChangeMinionById(id, s => { s.taunt = value; return s; });
    public void AddEffectIcon(ushort sequenceId, ushort effectId)
    {
        if (!TryFindMinion(sequenceId, out var list, out int i)) return;

        list[i].activeEffects.Add(effectId);   // közvetlen mutáció a közös listán
        list.Dirty(i);                          // ez küldi el
    }

    public void RemoveEffectIcon(ushort sequenceId, ushort effectId)
    {
        if (!TryFindMinion(sequenceId, out var list, out int i)) return;

        if (list[i].activeEffects.Remove(effectId))
            list.Dirty(i);                      // csak ha tényleg történt valami
    }
    public void ChangeMinionById(ushort sequenceId, Func<MinionState, MinionState> modify)
    {
        // Hős: nincs a board-listákban, külön SyncVar-ban él
        if (sequenceId < 2)
        {
            var pc = GetPlayer(sequenceId == 1);
            if (pc == null) return;
            pc.heroState.Value = modify(pc.heroState.Value);
            return;
        }
        Debug.Log($"ChangeMinionById HÍVVA: {sequenceId}");
        // Segédfüggvény a lista frissítéséhez, hogy ne ismételjük a kódot
        bool UpdateInList(SyncList<MinionState> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].sequenceId == sequenceId)
                {
                    // 1. Kivesszük az állapotot (Struct esetén ez egy másolat)
                    var minion = list[i];
                    // 2. Alkalmazzuk a módosítást és VISSZAírjuk a változóba
                    minion = modify(minion);
                    // 3. Visszahelyezzük a listába (Ez triggereli a SyncList frissítést)
                    list[i] = minion;

                    Debug.Log($"Minion {sequenceId} updated. HP: {list[i].currentHealth}");
                    return true;
                }
            }
            return false;
        }

        if (!UpdateInList(boardAlly))
        {
            UpdateInList(boardEnemy);
        }
    }
    private bool TryFindMinion(ushort sequenceId, out SyncList<MinionState> list, out int index)
    {
        for (int i = 0; i < boardAlly.Count; i++)
            if (boardAlly[i].sequenceId == sequenceId)
            {
                list = boardAlly; index = i; return true;
            }

        for (int i = 0; i < boardEnemy.Count; i++)
            if (boardEnemy[i].sequenceId == sequenceId)
            {
                list = boardEnemy; index = i; return true;
            }

        list = null; index = -1;
        return false;
    }
    public bool TryFindCard(ushort seqId, out PlayerController owner, out Zone zone, out int index)
    {
        var a = playerA.Value;
        if (a != null && a.TryFindCard(seqId, out zone, out index)) { owner = a; return true; }

        var b = playerB.Value;
        if (b != null && b.TryFindCard(seqId, out zone, out index)) { owner = b; return true; }

        owner = null; zone = Zone.None; index = -1;
        return false;
    }

    
    public CardState FindCardState(ushort seqId)
    {
        if (!TryFindCard(seqId, out var owner, out var zone, out int i))
        {
            Debug.LogError($"FindCardState: nincs ilyen lap: {seqId}");
            return default;
        }
        return owner.GetCard(zone, i);
    }

    /// <summary>A CardState módosításának EGYETLEN kapuja.</summary>
    public void ChangeCardById(ushort seqId, Func<CardState, CardState> modify)
    {
        if (!TryFindCard(seqId, out var owner, out var zone, out int i)) return;
        owner.SetCard(zone, i, modify(owner.GetCard(zone, i)));
    }
    public List<Effect> GetMinionEffects(ushort id)
    {
        var result = new List<Effect>();

        var list = GetMinionById(id).activeEffects;
        if (list == null) return result;          // kliensen null lehet

        foreach (ushort effectId in list)
            result.Add(EffectManager.Instance.GetEffectById(effectId));

        return result;
    }
    public byte isEnemy(ushort id,PlayerController player)=>IsEnemy(id,isAlly(player));
    public byte IsEnemy(ushort id,bool isAlly=true) 
    {
        if (boardEnemy.Any(i => i.sequenceId == id)) return isAlly? (byte)1 : (byte)0;
        if (boardAlly.Any(i => i.sequenceId == id)) return isAlly? (byte)0 : (byte)1;

        UnityEngine.Debug.LogError($"Target ID {id} not found on any board!");
        return 3; 
    }
    #endregion

    #region MinionLogic
    List<MinionLogic> minionLogics = new List<MinionLogic>();
    public IReadOnlyList<MinionLogic> MinionLogics => minionLogics;
    public MinionLogic CreateMinionLogic(ushort id)
    {
        var existing = GetMinionLogic(id);
        if (existing != null)
        {
            UnityEngine.Debug.LogError($"MinionLogic with sequenceId {id} already exists!");
            return existing;
        }

        var logic = new MinionLogic(id);
        minionLogics.Add(logic);
        return logic;
    }
    internal MinionLogic GetMinionLogic(ushort targetId)
    {
        return minionLogics.FirstOrDefault(m => m._sequenceId == targetId);
    }
    public bool IsEnemy(MinionLogic m)
    {
        return !boardAlly.Contains(
            GetMinionById(m._sequenceId));
    }


    #endregion



    #region TmpMinionCementary

    private List<(int index, ushort id)> tmpCementaryAlly = new();
    private List<(int index, ushort id)> tmpCementaryEnemy = new();

    public void PutInMinionToTmpCementary(int index, ushort id, bool ally)
    {
        if (ally) tmpCementaryAlly.Add((index, id));
        else tmpCementaryEnemy.Add((index, id));
    }

    public List<ushort> RestoreBoard(bool ally)
    {
        var tmpList = ally ? tmpCementaryAlly : tmpCementaryEnemy;
        var board = ally ? boardAlly : boardEnemy;

        var minionsAlive = new List<ushort>();
        foreach (var m in board) minionsAlive.Add(m.sequenceId);

        tmpList.Sort((a, b) => a.index.CompareTo(b.index));

        var restoredBoard = new List<ushort>();

        foreach (var dead in tmpList)
        {
            // ennyi élőnek kell elé kerülnie
            int difference = dead.index - restoredBoard.Count;

            for (int j = 0; j < difference && minionsAlive.Count > 0; j++)
            {
                restoredBoard.Add(minionsAlive[0]);
                minionsAlive.RemoveAt(0);
            }

            restoredBoard.Add(dead.id);
        }

        restoredBoard.AddRange(minionsAlive);
        return restoredBoard;
    }

    public void ClearCementary()
    {
        tmpCementaryAlly.Clear();
        tmpCementaryEnemy.Clear();
    }
    public bool IsDead(ushort id, bool ally)
    {
        var list = ally ? tmpCementaryAlly : tmpCementaryEnemy;
        foreach (var i in list)
            if (i.id == id) return true;
        return false;
    }

    public List<ushort> GetNeighbours(ushort id, bool ally)
    {
        var restored = RestoreBoard(ally);
        var result = new List<ushort>();

        int myPos = restored.IndexOf(id);
        if (myPos < 0) return result;

        // balra: az első élő
        for (int i = myPos - 1; i >= 0; i--)
        {
            if (IsDead(restored[i], ally)) continue;
            result.Add(restored[i]);
            break;
        }

        // jobbra: az első élő
        for (int i = myPos + 1; i < restored.Count; i++)
        {
            if (IsDead(restored[i], ally)) continue;
            result.Add(restored[i]);
            break;
        }

        return result;
    }
    
    #endregion
    #endregion

    //<<<<<<<<----------------------------------->>>>>>>>
    #region 2. JÁTÉKmechanika + események
    public void AddEventSystem(GameEvents gameEvents)
    {
        this.gameEvents = gameEvents;
        gameEvents.OnTurnStart += () => Debug.Log("Turn started");
    }
    public GameObject playerPrefab;

    void Update()
    {
        if (turnOff) return;

        if (IsServerInitialized && Input.inputString.Contains("ö"))
            DebugBoardState();

        // ── Innen csak az offline teszt: egy gépről vezéreljük mindkét játékost ──
        if (!offlineTestMode || !IsServerInitialized) return;

        if (Input.GetKeyDown(KeyCode.Return)) EndTurn();

        if (Input.GetKeyDown(KeyCode.D)) playerA.Value?.DrawCard();
        if (Input.GetKeyDown(KeyCode.U)) playerB.Value?.DrawCard();

        if (Input.GetKeyDown(KeyCode.F)) PlayFirstCard(playerA.Value);
        if (Input.GetKeyDown(KeyCode.G)) PlayFirstCard(playerB.Value);
    }

    private void PlayFirstCard(PlayerController pc)
    {
        if (pc == null || pc.hand.Count == 0) return;
        pc.PlayMinion(pc.hand[0]);
    }

    // GameManager
    public readonly SyncVar<int> turn = new(0);

    public int CurrentPlayerIndex => (turn.Value+1) % 2;

    public PlayerController CurrentPlayerController
        => CurrentPlayerIndex == 0 ? playerA.Value : playerB.Value;
    public bool IsPlayersTurn(PlayerController pc) => pc == CurrentPlayerController;

    [Server]
    public void EndTurn()
    {
        GameEvents.Instance.RaiseTurnEnd();
        graveyard.Execute();          // ha a körvégi effektek öltek

        StartTurn();
    }

    [Server]
    private void StartTurn()
    {
        turn.Value++;

        var pc = CurrentPlayerController;
        if (pc == null) return;

        pc.economy.StartTurn();

        foreach (var m in minionLogics)
            m.effectBag.TickExpiry(turn.Value);

        ResetAttacks(pc);

        pc.DrawCard();

        GameEvents.Instance.RaiseTurnStart();
        graveyard.Execute();

        Debug.Log($"Turn {turn.Value} — P{CurrentPlayerIndex + 1}");
    }

    [Server]
    private void ResetAttacks(PlayerController pc)
    {
        var board = pc.isEnemy.Value ? boardEnemy : boardAlly;
        for (int i = 0; i < board.Count; i++)
        {
            var s = board[i];
            if (s.canAttack) continue;

            var logic = GetMinionLogic(s.sequenceId);
            if (logic != null && logic.effectBag.ConsumeSleep()) continue;  // alszik még

            s.canAttack = true;
            board[i] = s;
        }
    }


    public void GameOver(bool isEnemy)
    {
        gameOverHandler.TriggerGameOver(isEnemy);
    }
    #endregion

//<<<<<<<<----------------------------------->>>>>>>>
    #region 3. EFFEKTEK 
    [Server]
    public void AddStats(ushort id, int atk, int hp)
    {
        var logic = GetMinionLogic(id);
        if (logic != null) { logic.Buff(atk, hp); return; }      // pályán

        ChangeCardById(id, cs =>                                  // kézben / pakliban
        {
            cs.attackBonus += (short)atk;
            cs.healthBonus += (short)hp;
            return cs;
        });

        // friss értékek a viszuálhoz
        var newCs = FindCardState(id);
        var def = CardManager.instance.GetCard(newCs.cardId);
        int newAttack = 0, newHealth = 0;
        if (def is MinionCard mc)
        {
            newAttack = mc.attack + newCs.attackBonus;
            newHealth = mc.health + newCs.healthBonus;
        }

        SendClientEvent(new ClientEvent
        {
            effectType = (ushort)Effect.Type.buff,
            targetIds = new ushort[] { id },
            value = atk,
            newValues = new int[] { newAttack, newHealth },
            doerId = id
        });
    }
    private bool _gameStarted;
    [Server]
    private void TryStartGame()
    {
        if (_gameStarted) return;
        if (playerA.Value == null || playerB.Value == null) return;

        _gameStarted = true;
        GameStart();
    }

    [Server]
    private void GameStart()
    {
        Debug.Log("GAME START");

        // Kezdőkéz
        for (int i = 0; i < 3; i++) playerA.Value.DrawCard();
        for (int i = 0; i < 4; i++) playerB.Value.DrawCard();   // a második több lapot kap

        // TODO: mulligan (lapcsere induláskor)
        // TODO: coin a második játékosnak

        turn.Value = -1;      // hogy a StartTurn ++ után 0 legyen → P1 kezd
        StartTurn();
    }
    #region EffectBagBusiness

    [Server]
    public void MoveCard(ushort cardSeqId, Zone from, Zone to)
    {
        // 1. A régi zóna képességei lebomlanak
        cardBags.Remove(cardSeqId, RemoveReason.ZoneChange);

        // 2. Az új zónáé felépül — bag csak Deck/Hand esetén kell
        if (to != Zone.Deck && to != Zone.Hand) return;
        var cs = FindCardState(cardSeqId);          // a kézben/pakliban lévő CardState

        var card = CardManager.instance.GetCard(cs.cardId);
        if (card == null) { Debug.LogError($"Nincs ilyen cardId: {cs.cardId}"); return; }

        if (card.effectIds == null || card.effectIds.Count == 0) return;

        Effect[] defs = EffectManagerClient.instance.GetEffectData(card.effectIds).ToArray();
        if (defs.Length == 0) return;

        var bag = cardBags.Create(cardSeqId);
        RegisterAbilities(bag, cardSeqId, defs, to);
    }

    public void RegisterAbilities(EffectBag bag, ushort ownerId, Effect[] effects, Zone currentZone)
    {
        foreach (var effect in effects)
        {
            if (effect.type == Effect.Type.taunt)
            {
                bag.Add(effect, ownerId, EffectRole.Aura, charges: -1);
                SetTaunt(ownerId, true);
                continue;
            }
            if (effect.type == Effect.Type.cleave)
            {
                bag.Add(effect, ownerId, EffectRole.Aura, charges: -1);
                continue;
            }
            if (effect?.triggers == null || effect.triggers.Length == 0) continue;
            if ((effect.activeZone & currentZone) == 0) continue;

            var t = effect.triggers[0];

            if (effect.type == Effect.Type.counter)
            {
                bag.Add(effect, ownerId, EffectRole.Guard,
                        charges: t.value, howOften: t.multiValue, toBlock: t.activity);
                continue;
            }
            
            if (TriggerConverter.ActiveEffectConverter(t.t, out var gameEvent, t))
            {
                bag.Add(effect, ownerId, EffectRole.Trigger);
                ushort capturedId = ownerId;

                if (gameEvent == GameEvents.EventType.MinionSummoned)
                {
                    GameEvents.Instance.AddEvent(capturedId, gameEvent,
                        (MinionLogic summoned) => {
                            if (!TriggerChecker.instance.IsDoerValid(t, summoned, capturedId)) return;
                            EffectRunner.Run(effect, capturedId); //  effectrunner helyett  DoRegistered EFfect és akkor ifso trigger
                                                                        // belül történik 
                        });
                }
                else
                {
                    GameEvents.Instance.AddEvent(capturedId, gameEvent,
                        () => EffectRunner.Run(effect, capturedId));
                }
            }/*
            if (TriggerConverter.ActiveEffectConverter(t.t, out var gameEvent))
            {
                bag.Add(effect, ownerId, EffectRole.Trigger);
                ushort capturedId = ownerId;
                GameEvents.Instance.AddEvent(capturedId, gameEvent,
                    () => EffectRunner.Run(effect, capturedId));
            }*/
            // se guard, se konvertálható trigger → battlecry, nem kap LiveEffect-et
        }
    }
    // GameManager
    #region CounterSpell / Block
    public enum BlockableBy { Target, Board }

    private static BlockableBy ScopeOf(Effect.Type kind)
    {
        switch (kind)
        {
            case Effect.Type.spell: return BlockableBy.Board;   // bárki elfoghatja a védekező oldalon
            default: return BlockableBy.Target;  // csak akit ér
        }
    }

    public bool CheckForCounter(Effect.Type kind, ushort targetId)
    {
        bool casterIsAlly = isAllyMinion(targetId);
        if (ScopeOf(kind) == BlockableBy.Target)
            return TryCounter(GetMinionLogic(targetId), kind);

        var defenders = casterIsAlly ? boardEnemy : boardAlly;

        for (int i = 0; i < defenders.Count; i++)             // pályasorrend = prioritás
            if (TryCounter(GetMinionLogic(defenders[i].sequenceId), kind))
                return true;                                  // az első elfogja, a többi nem fogy

        return false;
    }

    private bool TryCounter(MinionLogic logic, Effect.Type kind)
    {
        if (logic == null) return false;
        if (!logic.effectBag.TryConsumeGuard(kind)) return false;
        print("yuhuuu we blocked it!! ");
        //GameEvents.Instance.RaiseEffectCountered(logic, kind);
        return true;
    }
    [Server]
    public void SendClientEvent(ClientEvent e)
    {
        Debug.Log(
            $"ClientEvent küldve innen: {(Effect.Type)e.effectType}"
        );

        ReceiveEvent(e);
    }
    [ObserversRpc]
    public void ReceiveEvent(ClientEvent _event)
    {
        EffectClient.instance.AddEvent(_event);
    }

    #endregion

    #endregion

    public void DoEffect(EffectContext ctx)
    {
        EffectRunner.Run(ctx);
        SendClientEvent(ctx.ToClientEvent());

    }
    public void DoEffects(Effect[] effects, ushort doerId, PlayerController owner)
    {
        if (effects.Length == 0) return;
        foreach (var e in effects)
        {// if so trigger egyelőre szoló de ha több lesz könnyen megoldható
            // 1. Megkeressük az IfSo triggert manuálisan a tömbben
            Trigger ifSoTrigger = null;

            // Feltételezve, hogy az effect.triggers is egy Array
            for (int j = 0; j < e.triggers.Length; j++)
            {
                if (e.triggers[j].t == Trigger.time.ifso)
                {
                    ifSoTrigger = e.triggers[j];
                    break; // Megvan, nem kell tovább keresni
                }
            }

            // 2. Ha van IfSo feltétel, ellenőrizzük
            if (ifSoTrigger != null)
            {
                MinionLogic doer = GameManager.instance.GetMinionLogic(doerId);

                // Ha a központi IfSoTrigger hamisat ad, átugorjuk ezt az effektet
                if (!TriggerChecker.instance.IfSoTrigger(ifSoTrigger, doer, null))
                {
                    continue;
                }
            }
            DoEffect(e, doerId, owner,fromDoEffects:true);
        }graveyard.Execute();
    }
    public void DoEffect(Effect e, ushort doerId,  PlayerController owner, List<ushort> targets = null, ushort extraValue = 0,bool fromDoEffects=false)
    {
       // if(targets == null)
       // List<ushort> targets = TargetingCenter.GetTargets(e, doerId, owner);
        var ctx = new EffectContext(e, doerId, targets,source:owner);

        EffectRunner.Run(ctx); // Szerver matek
        Debug.Log($"[Send] {ctx.effect.type}, targetIds: {string.Join(",", ctx.targetIds)}");
        if (ctx.effect.type!=Effect.Type.damage && ctx.effect.type!=Effect.Type.summon && ctx.effect.type !=  Effect.Type.doubleStats)
        SendClientEvent(ctx.ToClientEvent()); // Kliens mozi
        if (!fromDoEffects) graveyard.Execute();
    }

    #endregion

//<<<<<<<<----------------------------------->>>>>>>>
    #region 4. Harc
    [Server]
    public void ExecuteAttack(ushort attackerId, ushort deffenderId)
    {// ez a metodus kihelyezhető máshova ne mindenért a game manager feleljen.
        //ushort? overriddenTarget = EffectRunner.RunBeforeAttack(attackerId, defenderId);
        //ushort actualDefenderId = overriddenTarget ?? defenderId;
        MinionLogic attacker = GetMinionLogic(attackerId);
        var defBoard = isAllyMinion(deffenderId)?boardAlly:boardEnemy;
        bool tauntExists = false;
        foreach (var m in defBoard) if (m.taunt) { tauntExists = true; break; }

        if (tauntExists)
        {
            var target = GetMinionById(deffenderId);
            if (!target.taunt)
            {
                Debug.Log("Taunt miatt elutasítva.");
                return;
            }
        }
        // Effect[] beforeAttack = TriggerChecker.instance.CheckTrigger(Trigger.time.instant, Effect.Type.attack, attackerId).ToArray();
        if (attacker == null)return;
        if (CheckForCounter(Effect.Type.attack, deffenderId))
            return;
        attacker.Attack(GetMinionById(attackerId).attack, deffenderId);

    }
    
    public void CancelAttack()
    {
        Arrow3DPointer.instance.TurnOff();

    }

    #endregion

//<<<<<<<<----------------------------------->>>>>>>>
    #region 5.  HÁLÓZAT
    void Awake()
    {
        instance = this;
        if (turnOff) return;
        networkManager = FindObjectOfType<NetworkManager>();


        if (/*!online &&*/ offlineTestMode)
        {
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
        }
    }
    private bool _netStarted=false;

    
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (turnOff) return;
        // Itt fut le a játék inicializálása, ami eddig az Awake-ben volt.
        gameOverHandler = GetComponent<GameOverHandler>();
        GameEvents.Reset();
        gameEvents = GameEvents.Instance;
        Hero a = ScriptableObject.CreateInstance<Hero>();
        gameState = new GameState();
        Init(testDeck, a, testDeck, a);

    }
    public override void OnStartClient()
    {
        base.OnStartClient();
    }
    void Init(Deck p1_d, Hero p1_h, Deck p2_d, Hero p2_h)
    {
        
           // idejönnek majd a herok p1_h = new Hero();

        gameState.players[0].deck = p1_d;
        gameState.players[1].deck = p2_d;
        gameState.players[1].hero = p2_h;
        gameState.players[0].hero = p1_h;

       // playerA.Value.InitHero(30);
       // playerB.Value.InitHero(30);
        
        if (offlineTestMode)
        {
            GameObject player2GO = Instantiate(playerPrefab);
            NetworkObject player2NO = player2GO.GetComponent<NetworkObject>();
            if (player2NO != null)
            {
                ServerManager.Spawn(player2NO);
                playerB.Value = player2GO.GetComponent<PlayerController>();
                playerB.Value.isDummy = true;
            }
            // A PlayerController-ek Init metódusát a szerver hívja meg.
            playerB.Value.Init(gameState.players[1], false, this);
            player2GO.transform.parent = transform;
        }

        /*GameObject player = new GameObject("player1");
        player.transform.parent = transform;
        playerA.Value = player.AddComponent<PlayerController>();
        playerA.Value.Init(gameState.players[0], true, this);
        player = new GameObject("player2");
        player.transform.parent = transform;
        playerB.Value = player.AddComponent<PlayerController>();
        playerB.Value.Init(gameState.players[1], false, this);*/

    }
    
    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        // A kliens PlayerController-e itt regisztrálja magát
        //players.Add(player);
        if (playerA.Value == null)
        {
            playerA.Value = player;
            player.isEnemy.Value = false;
            player.transform.parent = transform;
            player.Init(gameState.players[0], true, this);
            player.RoleAssignedTargetRpc(player.Owner, player.isEnemy.Value);
        }
        else
        {
            playerB.Value = player;
            player.isEnemy.Value = true;
            player.Init(gameState.players[1], false, this);
            player.RoleAssignedTargetRpc(player.Owner, player.isEnemy.Value);
        }
        //TryStartGame();
    }
    #endregion

//<<<<<<<<----------------------------------->>>>>>>>

    #region 6. ÁLLAPOTMÓDOSÍTÓK (State Mutators) LEKÉRDEZÉSEK & KERESŐK

    public void RemoveFromBoard(ushort currentId)
    {
        MinionLogic logic = GetMinionLogic(currentId);
        MinionState state = GetMinionById(currentId);

        // 1. Levesszük a pályáról — a deathrattle így már szabad helyet lát,
        //    ha maga idéz valamit a helyére.
        boardAlly.Remove(state);
        boardEnemy.Remove(state);

        // 2. Kirobbantjuk a halált — a feliratkozások MÉG élnek,
        //    tehát a deathrattle és az idegen "amikor egy lény meghal" triggerek lefutnak.
        // GameEvents.Instance.RaiseMinionDied(logic);

        // 3. Csak ezután bontjuk le. Innentől a lény nem létezik.
        RemoveMinionLogic(logic, RemoveReason.Death);
    }

    public void RemoveMinionLogic(MinionLogic logic, RemoveReason reason)
    {
        if (logic == null) return;

        // TODO: ha lesz olyan képesség, ami MÁS lényre rak effektet (kölcsön-buff,
        // kívülről adott pajzs), akkor itt kell visszavonni:
        //   foreach (var m in minionLogics) m.effectBag.RemoveBySource(logic._sequenceId);

        logic.effectBag.DisposeAll(reason);
        minionLogics.Remove(logic);
    }
    internal MinionView GetMinionView(ushort v)
    {
        return
        GetComponent<BoardManager>().GetMinion(v);
    }
    #region GetPlayer methods

    [Server]
    public PlayerController GetOwnerOf(ushort sequenceId)
    {
        if (sequenceId < 2) return GetPlayerByIndex(sequenceId);      // hős

        for (int i = 0; i < boardAlly.Count; i++)
            if (boardAlly[i].sequenceId == sequenceId) return playerA.Value;

        for (int i = 0; i < boardEnemy.Count; i++)
            if (boardEnemy[i].sequenceId == sequenceId) return playerB.Value;

        return null;
    }
    public bool isAlly(PlayerController player)
    {
        return player==playerA.Value;
    }
    
    public PlayerController GetEnemy(PlayerController p)
    {
        return p == playerA.Value ? playerB.Value : playerA.Value;
    }
    public PlayerController GetPlayer(bool isEnemy = false)
    {
        return isEnemy ? GetOpponentPlayerController() : GetLocalPlayerController();
    }
    public PlayerController GetplayerByTurn(bool isEnemy = false)
    {
        return isEnemy
            ? GetControllerOf(gameState.OpponentPlayer)
            : GetControllerOf(gameState.CurrentPlayer);
    }
    public PlayerController GetLocalPlayerController()
    {
        return playerA.Value != null && playerA.Value.IsOwner ? playerA.Value : playerB.Value;
    }
    public bool AreWeHomePlayer()
    {
        return playerA.Value.IsOwner;
    }

    public PlayerController GetOpponentPlayerController()
    {
        return localPlayerIndex == 0 ? playerB.Value : playerA.Value;
    }
    public ushort GetHeroId(PlayerController player,bool enemy=false)
    {
        ushort id;
        if (player == playerA.Value) { id = enemy ? (ushort)1 : (ushort)0; }
        else { id = enemy ? (ushort)0 : (ushort)1; }
        return id;
    }
    public PlayerController GetControllerOf(PlayerState state)
    {
        if (state == playerA.Value.state) return playerA.Value;
        if (state == playerB.Value.state) return playerB.Value;
        Debug.LogWarning("Unknown PlayerState!");
        return null;
    }
    public PlayerController OtherPlayer(PlayerController playerController)
    {
        return playerController == playerA.Value ? playerB.Value : playerA.Value;
    }
    public PlayerController GetPlayerByIndex(int index)
    => index == 0 ? playerA.Value : playerB.Value;
    #endregion
    internal bool IsMyTurn()
    {
        return true;
        return localPlayerIndex == 0 ?turn.Value % 2 == 0:turn.Value %2==1;
    }
    public bool isAllyTurn()
    {
        return turn.Value % 2 == 0;
    }

    
    public List<Card> GetPlayerDeck()
    {
        return gameState.players[0].deck.deck.Concat(gameState.players[1].deck.deck).ToList();
    }

 
    
    public Dictionary<int, MinionView> minionsUI;
    public void AddMinionUIDictionary(int minion, MinionView minionView) { minionsUI.Add(minion, minionView); }
    public void DeleteMinionUIDictionary(int minion) { minionsUI.Remove(minion); }
    public Card GetCardById(ushort id) 
    {
            return testDeck.deck[0];
    }

    public readonly CardBags cardBags = new();

    // P1: 1–7999, P2: 8000+. Csak diagnosztika — ránézésre látszik, kié volt EREDETILEG.
    // A logika SOHA ne ebből döntsön: lopott lapnál az ID nem változik.
    private const ushort P2_START = 8000;

    private ushort _nextP1 = 2;      // 0 = "érvénytelen/nincs"
    private ushort _nextP2 = P2_START;

    public ushort NextCardId(bool isPlayerOne)
    {
        if (isPlayerOne) return _nextP1++;
        return _nextP2++;
    }
    #endregion

//<<<<<<<<----------------------------------->>>>>>>>
    #region 7.Debug
    [Server]
    public void DebugBoardState()
    {
        Debug.Log("--- Tábla Állapota ---");

        // Szövetséges minionok
        Debug.Log("--- Szövetséges Minionok ---");
        for (int i = 0; i < boardAlly.Count; i++)
        {
            MinionState minion = boardAlly[i];
            Debug.Log($"Index: {i}, SequenceId: {minion.sequenceId},  Attack: {minion.attack}, Health: {minion.currentHealth}, CanAttack: {minion.canAttack}");
            Debug.Log($"minion:{ minion.activeEffects.Count}");
        }

        // Ellenséges minionok
        Debug.Log("--- Ellenséges Minionok ---");
        for (int i = 0; i < boardEnemy.Count; i++)
        {
            MinionState minion = boardEnemy[i];
            Debug.Log($"Index: {i}, SequenceId: {minion.sequenceId},  Attack: {minion.attack},Health: {minion.currentHealth}, CanAttack: {minion.canAttack}");
        }
    }
    #endregion
    
}

