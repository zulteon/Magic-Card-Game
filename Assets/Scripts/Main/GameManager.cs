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
using static Trigger;
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
    /// <summary>
    ///  This is only for Albatrosz, to watche each other death.
    /// </summary>
    /// <param name="watcherId"></param>
    /// <param name="watchedId"></param>
    /// <param name="effect"></param>
    public void RegisterWatchedDeath(ushort watcherId, ushort watchedId, Effect effect)
    {
        var logic = GetMinionLogic(watcherId);
        if (logic == null || effect == null) return;

        var live = logic.effectBag.Add(effect, watcherId, EffectRole.Trigger);
        live.watchedId = watchedId;

        var watcherOwner = GetOwnerOf(watcherId);

        GameEvents.Instance.AddEvent(watcherId, GameEvents.EventType.MinionDied,
            (MinionLogic dead) => {
                var watcherLogic = GetMinionLogic(watcherId);
                if (watcherLogic == null || watcherLogic.effectBag.IsLocked) return;   

                if (dead._sequenceId != watchedId) return;
                EffectRunner.Run(effect, watcherId, source: watcherOwner);
            });
    }

    //do cementary minions needed?
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
    public List<MinionLogic> lockedMinions = new List<MinionLogic>();
    public MinionLogic allyHeroLogic;
    public MinionLogic enemyHeroLogic;
    public IReadOnlyList<MinionLogic> MinionLogics => minionLogics;
    public void CreateHeroMinionLogic(bool isEnemy)
    {
        if (allyHeroLogic != null && !isEnemy) return;
        if (enemyHeroLogic != null && isEnemy) return;
        print("JUHUUU KREATING A Logic for hero");
        var logic = new MinionLogic(isEnemy?(ushort)1:(ushort)0);
        if (!isEnemy) allyHeroLogic = logic;
        if (isEnemy) enemyHeroLogic = logic;
    }
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
        if(targetId<2)
            return targetId==0?allyHeroLogic : enemyHeroLogic;
        foreach( var m in minionLogics) if (m._sequenceId == targetId)return m;
        foreach ( var m in tmpCementary) if (m._sequenceId == targetId) return m;
        foreach ( var m in lockedMinions) if (m._sequenceId == targetId) return m;
        return default;
    }
    public List<MinionLogic> SortByBodyGuard(List<MinionLogic> minions)
    {
        var guardedIds = new HashSet<ushort>();    // akiket valaki véd
        var protectorIds = new HashSet<ushort>();  // akik védenek valakit

        // 1. Megnézzük a kapcsolatokat
        foreach (var minion in minions)
        {
            ushort protectorId = minion.effectBag.GetProtector();

            if (protectorId == 0)
                continue;

            guardedIds.Add(minion.sequenceId);
            protectorIds.Add(protectorId);
        }
        if (protectorIds.Count == 0)
            return minions;

        // 2. Négy érthető kategória
        var normal = new List<MinionLogic>();
        var guardedOnly = new List<MinionLogic>();
        var guardedAndProtector = new List<MinionLogic>();
        var protectorOnly = new List<MinionLogic>();


        // Az EREDETI sorrendet tartjuk meg kategórián belül
        foreach (var minion in minions)
        {
            ushort id = minion.sequenceId;

            bool isGuarded = guardedIds.Contains(id);
            bool isProtector = protectorIds.Contains(id);

            MinionLogic logic =
                GameManager.instance.GetMinionLogic(id);

            if (logic == null)
                continue;

            if (!isGuarded && !isProtector)
            {
                normal.Add(logic);
            }
            else if (isGuarded && !isProtector)
            {
                guardedOnly.Add(logic);
            }
            else if (isGuarded && isProtector)
            {
                guardedAndProtector.Add(logic);
            }
            else // !isGuarded && isProtector
            {
                protectorOnly.Add(logic);
            }
        }


        // 3. Sebzési sorrend
        var result = new List<MinionLogic>();

        result.AddRange(normal);
        result.AddRange(guardedOnly);
        result.AddRange(guardedAndProtector);
        result.AddRange(protectorOnly);

        return result;
    }
    public void CheckLockedMinions()
    {
        for (int i = lockedMinions.Count - 1; i >= 0; i--)
        {
            var logic = lockedMinions[i];
            if (!logic.effectBag.TickLock()) continue;

            lockedMinions.RemoveAt(i);

            var state = logic.effectBag.LockedState;
            bool isAlly = logic.effectBag.LockedIsAlly;

            if (isAlly) boardAlly.Add(state);
            else boardEnemy.Add(state);

            SendClientEvent(new ClientEvent
            {
                effectType = (ushort)Effect.Type.summon,
                targetIds = new ushort[] { state.sequenceId },
                newValues = new[] { isAlly ? 1 : 0 }
            });
        }
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
    internal List<MinionLogic> tmpCementary=new();

    public void PutInMinionToTmpCementary(int index, ushort id, bool ally,MinionLogic m)
    {
        if (ally) tmpCementaryAlly.Add((index, id));
        else tmpCementaryEnemy.Add((index, id));
        tmpCementary.Add(m);
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
        EffectClient.instance.TurnEndObserversRpc();
        GameEvents.Instance.RaiseTurnEnd();
        int cantAttackForTurns=GetplayerByTurn().CantAttackForTurn;
        if (cantAttackForTurns > 0)
            GetplayerByTurn().CantAttackForTurn--;
        graveyard.Execute();          // ha a körvégi effektek öltek
        
        StartTurn();
    }

    [Server]
    private void StartTurn()
    {
        EffectClient.instance.TurnStartObserversRpc();
        turn.Value++;

        var pc = CurrentPlayerController;
        if (pc == null) return;

        print(pc.economy.ToString());
        pc.economy.StartTurn();

        CheckLockedMinions();
        foreach (var m in minionLogics)
            m.effectBag.TickExpiry();

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
            if ((currentZone & Zone.Board) != 0)
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
                if (effect.type == Effect.Type.unattackable)
                {
                    bag.Add(effect, ownerId, EffectRole.Aura, charges: -1);
                    continue;
                }
            }
            if (effect?.triggers == null || effect.triggers.Length == 0) continue;
            if ((effect.activeZone & currentZone) == 0) continue;

            var t = effect.triggers[0];
            if (t.t == Trigger.time.before)
            {
                bag.Add(effect, ownerId, EffectRole.Aura,
                        charges: t.value, howOften: t.multiValue);
                continue;
            }
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

                if (TriggerConverter.EventHasMinion(gameEvent))
                { // az if es dolgokat egyhelyre gyüjthetjük a gameevents  trigger converterbe
                    var myLogic = GameManager.instance.GetMinionLogic(capturedId);
                    if (myLogic != null && myLogic.effectBag.IsLocked ||bag.IsLocked) continue;
                    bool checkSameCard = gameEvent == GameEvents.EventType.MinionBuffed // a végtelen buffolási lánc szakitás
                  && effect.type == Effect.Type.buff;
                    GameEvents.Instance.AddEvent(capturedId, gameEvent,
                        (MinionLogic summoned) => {
                            if (!TriggerChecker.instance.IsDoerValid(t, summoned, capturedId)) return;
                            if (checkSameCard)
                            {
                                var me = GameManager.instance.GetMinionById(capturedId);
                                var him = GameManager.instance.GetMinionById(summoned._sequenceId);
                                if (me.cardId == him.cardId) return;
                            }
                            EffectRunner.Run(effect, capturedId); //  effectrunner helyett  DoRegistered EFfect és akkor ifso trigger
                                                                        // belül történik 
                        });
                }
                else
                {
                    GameEvents.Instance.AddEvent(capturedId, gameEvent,
                        () => {
                            var myLogic = GameManager.instance.GetMinionLogic(capturedId);
                            if (myLogic == null || myLogic.effectBag.IsLocked) return;

                            EffectRunner.Run(effect, capturedId);
                        });
                }
            }
            Debug.Log($"[Register] {effect.effectId} ownerId={ownerId} zone={currentZone}");
            /*
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


    #endregion
    #region EventRendszer 
    private List<ClientEvent> _batch;
    private int _batchDepth;

    public void StartEventQueue()
    {
        if (_batchDepth == 0) _batch = new List<ClientEvent>();
        _batchDepth++;
    }

    public void FinishEventQueue()
    {
        _batchDepth--;
        if (_batchDepth > 0) return;          // beágyazott hívásban vagyunk
        if (_batch == null || _batch.Count == 0) { _batch = null; return; }

        var merged = MergeByType(_batch);
        _batch = null;                        // ELŐBB nullázzuk!
        SendClientEvent(merged.ToArray());
    }
    private List<ClientEvent> MergeByType(List<ClientEvent> list)
    {
        var order = new List<ushort>();                    // megőrzi az első előfordulás sorrendjét
        var byType = new Dictionary<ushort, ClientEvent>();

        foreach (var e in list)
        {
            if (!byType.TryGetValue(e.effectType, out var acc))
            {
                byType[e.effectType] = e;
                order.Add(e.effectType);
                continue;
            }

            acc.targetIds = Concat(acc.targetIds, e.targetIds);
            acc.newValues = Concat(acc.newValues, e.newValues);
            byType[e.effectType] = acc;                    // struct esetén KÖTELEZŐ
        }

        var result = new List<ClientEvent>();
        foreach (var t in order) result.Add(byType[t]);
        return result;
    }

    private static T[] Concat<T>(T[] a, T[] b)
    {
        if (a == null) return b;
        if (b == null) return a;
        var r = new T[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }
    [ObserversRpc]
    private void SendClientEvent(ClientEvent[] events)
    {
        EffectClient.instance.AddEventBatch(events);
    }
    [Server]
    public void SendClientEvent(ClientEvent e)
    {
        if (_batchDepth > 0) { _batch.Add(e); return; }
        Debug.Log(
            $"ClientEvent küldve innen: {(Effect.Type)e.effectType}"
        );

        ReceiveEvent(e);
    }
    [ObserversRpc]
    public void ReceiveEvent(ClientEvent _event)
    {
        EffectClient.instance.AddEventBatch(new[]{_event});
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
        if(effects.Length>1)
            StartEventQueue();

        foreach (var e in effects)
        {// if so trigger egyelőre szoló de ha több lesz könnyen megoldható
            // 1. Megkeressük az IfSo triggert manuálisan a tömbben
            
            DoEffect(e, doerId, owner,fromDoEffects:true);
        }
        FinishEventQueue();
        graveyard.Execute();
    }
    public void DoEffect(Effect e, ushort doerId,  PlayerController owner, List<ushort> targets = null, ushort extraValue = 0,bool fromDoEffects=false)
    {
       // if(targets == null)
       // List<ushort> targets = TargetingCenter.GetTargets(e, doerId, owner);
        EffectContext ctx = new EffectContext(e, doerId, targets,source:owner);
        Trigger[] ifsoTriggers = System.Array.FindAll(e.triggers, t => t.t == Trigger.time.ifso);
        if (ifsoTriggers.Length > 0)
        {
            MinionLogic target = targets?.Count > 0
                ? GetMinionLogic(targets[0])
                : null;

            if (!TriggerChecker.instance.IfSoTrigger(ifsoTriggers[0], GetMinionLogic(doerId), target))
                return;
        }
        if (e.targetCondition != null &&
        e.targetCondition.sub != Trigger.subject.None &&
        ctx.targetIds != null)
        {
            ctx.targetIds = ctx.targetIds
                .Where(id => GameManager.instance.MeetsTargetCondition(
                    e.targetCondition,
                    GameManager.instance.GetMinionById(id)))
                .ToArray();

            // ha minden célpont kiesett, az effekt nem fut le
            if (ctx.targetIds.Length == 0) return;
        }
        EffectRunner.Run(ctx); // Szerver matek
        try
        {
            Debug.Log($"[Send] {ctx.effect.type}, targetIds: {string.Join(",", ctx.targetIds)}");
        }
        catch { }
        if (ctx.effect.type!=Effect.Type.damage  && ctx.effect.type !=  Effect.Type.doubleStats && Effect.Type.minionSwap!=ctx.effect.type)
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
        var defLogic = GetMinionLogic(deffenderId);
        if (defLogic != null && defLogic.effectBag.Has(Effect.Type.unattackable))
        {
            Debug.Log("Untargetable — támadás elutasítva.");
            return;
        }
        var beforeEffects = attacker.effectBag
            .ConsumeByTrigger(Trigger.time.before, Effect.Type.attack);

        if (beforeEffects.Count > 0)
            DoEffects(beforeEffects.ToArray(), attackerId, GetOwnerOf(attackerId));
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


    }
    
    [Server]
    public void RegisterPlayer(PlayerController player,ushort[] deckIds)
    {
        // A kliens PlayerController-e itt regisztrálja magát
        //players.Add(player);
        if (playerA.Value == player || playerB.Value == player)
        {
            Debug.LogWarning("Ez a player már regisztrálva van, duplikált hívás.");
            return;
        }
        if (playerA.Value == null)
        {
            playerA.Value = player;
            player.isEnemy.Value = false;
            player.transform.parent = transform;
            player.Init(gameState.players[0], true, this,offlineTestMode?null:deckIds);
            player.RoleAssignedTargetRpc(player.Owner, player.isEnemy.Value);
        }
        else
        {
            playerB.Value = player;
            player.isEnemy.Value = true;
            player.Init(gameState.players[1], false, this,offlineTestMode?null:deckIds);
            player.RoleAssignedTargetRpc(player.Owner, player.isEnemy.Value);
        }
        TryStartGame();
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
    public void RemoveFromBoardSilently(ushort currentId)
    {
        MinionState state = GetMinionById(currentId);
        boardAlly.Remove(state);
        boardEnemy.Remove(state);
    }
    public static Vector2Int GetMinionBuff(ushort cardId, MinionLogic m)
    {
        var card = CardManager.instance.GetMinion(cardId);
        if (card == null || m == null) return Vector2Int.zero;

        int attackDiff = m.attack - card.attack;
        int healthDiff = (int)m.maxhealth - card.health;

        return new Vector2Int(attackDiff, healthDiff);
    }
    public void RemoveMinionLogic(MinionLogic logic, RemoveReason reason)
    {
        if (logic == null) return;

        // TODO: ha lesz olyan képesség, ami MÁS lényre rak effektet (kölcsön-buff,
        // kívülről adott pajzs), akkor itt kell visszavonni:
        foreach (var m in minionLogics) m.effectBag.RemoveBySource(logic._sequenceId);

        logic.effectBag.DisposeAll(reason);
        minionLogics.Remove(logic);
    }
    public bool MeetsTargetCondition(Trigger condition, MinionState state)
    {
        switch (condition.sub)
        {
            case Trigger.subject.isDamaged:
                return state.currentHealth < (state.maxHealth);

            case Trigger.subject.Attack:
                switch (condition.cond)
                {
                    case Trigger.conditions.less: return state.attack < condition.value;
                    case Trigger.conditions.equals: return state.attack == condition.value;
                    case Trigger.conditions.more: return state.attack > condition.value;
                }
                break;

            case Trigger.subject.Health:
                switch (condition.cond)
                {
                    case Trigger.conditions.less: return state.currentHealth < condition.value;
                    case Trigger.conditions.equals: return state.currentHealth == condition.value;
                    case Trigger.conditions.more: return state.currentHealth > condition.value;
                }
                break;
        }
        return true;
    }
    [SerializeField]
    public HeroView homeHeroView,enemyHeroView;
    internal MinionView GetMinionView(ushort v)
    {
        if (v < 2)
        {
            return v==0 ? homeHeroView : enemyHeroView;
        }
        return
        BoardManager.instance.GetMinion(v);
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
    {if (turn.Value < 0) return true;
        PlayerController player =
        GetLocalPlayerController();

        if (player == null)
            return false;

        bool enemyTurn =
            turn.Value % 2 == 1;

        return player.isEnemy.Value == enemyTurn;
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

    internal int GetHandCount(bool v)
    {
        return v ? playerA.Value.hand.Count : playerB.Value.hand.Count;
    }
    #endregion

}

