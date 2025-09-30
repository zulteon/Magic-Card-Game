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
    //public static event Action OnTurnEnd,OnTurnStart;
    public static GameManager instance;

    private GameEvents gameEvents;
    GameOverHandler gameOverHandler;
    private int localPlayerIndex=0;
    public Effect TestEffect;
    public bool online = true;
    public NetworkManager networkManager;
    public GameObject liveCardPrefab;
    public bool offlineTestMode;
    public bool turnOff = false;
    #region Board
    public readonly SyncList<MinionState> boardAlly=new();
    public readonly SyncList<MinionState> boardEnemy=new(); 
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
//Need to implement
    public  MinionState GetMinionById(ushort sequenceId)
    {
       foreach(var minion in boardAlly.Concat(boardEnemy))
        {
            if(minion.sequenceId == sequenceId) return  minion;
        }
        return default;
        //foreach in heros
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
    
    public void ChangeMinionById(ushort sequenceId, Func<MinionState, MinionState> modify)
    {
        // Ally board
        for (int i = 0; i < boardAlly.Count; i++)
        {
            if (boardAlly[i].sequenceId == sequenceId)
            {
                var minion = boardAlly[i];
                minion=modify( minion);  // Lambda-val módosítás
                print(minion.currentHealth);

                boardAlly[i] = minion;
                return;
            }
        }

        // Enemy board
        for (int i = 0; i < boardEnemy.Count; i++)
        {
            if (boardEnemy[i].sequenceId == sequenceId)
            {
                var minion = boardEnemy[i];
                modify(minion);
                boardEnemy[i] = minion;
                return;
            }
        }
    }

    // List<ILiveTarget> ally { get { return board[borderBoardEnemy:]} set; }
    void Awake()
    {
        instance = this;
        if (turnOff) return;
        networkManager = FindObjectOfType<NetworkManager>();
        

        if (!online)
        {
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (turnOff) return;
        // Itt fut le a játék inicializálása, ami eddig az Awake-ben volt.
        gameOverHandler = GetComponent<GameOverHandler>();
        gameEvents = new GameEvents();
        Hero a = ScriptableObject.CreateInstance<Hero>();
        gameState = new GameState();
        Init(testDeck, a, testDeck, a);

    }
    public enum Phase { ready,animation,targeting }
    public Phase phase = Phase.ready;
    GameState gameState;

    public readonly SyncVar<PlayerController> playerA = new SyncVar<PlayerController>();
    public readonly SyncVar<PlayerController> playerB = new SyncVar<PlayerController>();
    public Deck testDeck;
    
    private int turn = 1;
    public void AddEventSystem(GameEvents gameEvents)
    {
        this.gameEvents = gameEvents;
        gameEvents.OnTurnStart += () => Debug.Log("Turn started");
    }
    public GameObject playerPrefab;
    void Init(Deck p1_d,Hero p1_h,Deck p2_d,Hero p2_h)
    {
        if(p1_h==null || p2_h==null)
            p1_h = new Hero();
            p2_h = new Hero();
            
        gameState.players[0].deck = p1_d;
        gameState.players[1].deck = p2_d;
        gameState.players[1].hero = p2_h;
        gameState.players[0].hero = p1_h;

        
        GameObject player2GO = Instantiate(playerPrefab);
        NetworkObject player2NO = player2GO.GetComponent<NetworkObject>();
        if (offlineTestMode)
        {
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
    void Update()
    {if (turnOff) return;
        // Ez csak demo, nyomj entert a kör végéhez:
        if (Input.GetKeyDown(KeyCode.Return))
        {
            EndTurn();
        }
        if (Input.GetKeyDown(KeyCode.D) && IsServer)
        {
            print("drawwweA"+playerA.Value);
            playerA.Value.DrawCard();

        }
        if (Input.GetKeyDown(KeyCode.U) && IsServer)
        {
            print("drawwB"+playerB.Value);

            playerB.Value.DrawCard();

        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            print("huhuh");
            playerB.Value.PlayMinion(playerB.Value.hand[0]);
        }
        /*if (IsServer && Input.inputString.Contains("ö"))
        {
            DebugBoardState();
        }   */

    }
    
    public void StartTurn()
    {
        Debug.Log($"Turn {turn} ended.");
        gameEvents.RaiseTurnStart();
    }

    public void EndTurn()
    {
        
        Debug.Log($"Turn {turn} ended.");
        gameEvents.RaiseTurnEnd();
        turn++;
    }

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
    public void GameOver(bool isEnemy)
    {
        gameOverHandler.TriggerGameOver(isEnemy);
    }
    public ILiveTarget currentAtacker;
    [ServerRpc]
    public void DamageServerRpc(ushort attackerId,ushort attackedId)
    {
        ushort damage=(ushort)
        GetMinionById(attackerId).attack;
    }
    public void AttackWith(ILiveTarget attacker,Vector3 position)// selection phase
    {
        Arrow3DPointer.instance.SetArrow(position);
        phase = Phase.targeting;
        currentAtacker = attacker;
    }
    public void StartSelectPhase(ILiveTarget attacker)
    {
        Arrow3DPointer.instance.SetArrow(attacker.GetTransform().position);
        phase = Phase.targeting;
        currentAtacker = attacker;
    }

    public void FinishAttack()
    {
        currentAtacker = null;
        
        phase = 0;
    }
    
    public void CancelAttack()
    {
        currentAtacker=null;
        Arrow3DPointer.instance.TurnOff();

    }

   

    //################################

    #region GetPlayer methods
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
        return localPlayerIndex == 0 ? playerA.Value : playerB.Value;
    }

    public PlayerController GetOpponentPlayerController()
    {
        return localPlayerIndex == 0 ? playerB.Value : playerA.Value;
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
    #endregion
    internal bool IsMyTurn()
    {
        
        return localPlayerIndex == 0 ?turn % 2 == 0:turn%2==1;
    }
    public  IEnumerator AttackIt(ILiveTarget attacker, ILiveTarget victim,bool isHome=true)
    {
        Transform a = attacker.GetTransform();
        Transform b=victim.GetTransform();
        print("ATACKIT#####!!!!!!!!!!!!!!!!!!!"+a.name+":"+b.name);
        
        Vector3 startpos = a.transform.position;
        float t = 0.2f;
        float allT = t;
        Vector3 firstStation = a.position + new Vector3(0f, isHome ? 0.078f : -0.078f, 0f);
        while (t > 0)
        {
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(a.position, firstStation, (allT - t) / allT);
            yield return null;
        }
        float dist = Vector3.Distance(a.transform.position, b.transform.position);
        Vector3 tmp = a.position;
        t = 0.5f; allT = t;
        while (t > 0)
        {
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(tmp, b.position, (allT - t) / allT);
            yield return null;
        }
        // SoundManager.inst.Hit();
        //damage
        
        Vector3 endPos = a.position;
        t = 0.7f; allT = t;
        while (t > 0)
        {
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(b.position, firstStation, (allT - t) / allT);
            yield return null;
        }
        t = 0.2f; allT = t;
        while (t > 0)
        {
            t -= Time.deltaTime;
            a.position = Vector3.Lerp(firstStation, startpos, (allT - t) / allT);
            yield return null;
        }
        a.position = startpos;
        int victimAttack=victim.Attack;
        victim.Damage(attacker.Health);
        attacker.Damage(victimAttack);
        
        FinishAttack();
    }

    // A RequireOwnership = false kulcsfontosságú, mert a GameManagernek nincs tulajdonosa.

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (turnOff) return;
        // A kliens példány iratkozik fel a SyncList-ekre.
        //boardAlly.OnChange += BoardManager.instance.OnBoardChangeHome;
        //boardEnemy.OnChange += BoardManager.instance.OnBoardChangeEnemy;
    }
    [ServerRpc]
    public void StartGame()
    {
        // A deck-et itt kellene betölteni egy adatbázisból vagy fájlból
        List<Card> playerDeck = GetPlayerDeck();

        // Szerver oldalon betöltjük a CardManager-t a paklival
        //CardManager.instance.LoadCardsFromJson(playerDeck);

        RpcLoadCardsOnClient(playerDeck.Select(card => card.cardId).ToList());
    }
    public List<Card> GetPlayerDeck()
    {
        return gameState.players[0].deck.deck.Concat(gameState.players[1].deck.deck).ToList();
    }
    [ObserversRpc]
    public void RpcLoadCardsOnClient(List<ushort> cardIds)
    {
        // A kliens kapja a hívást és betölti a kártyákat a saját CardManager-be
        List<Card> playerDeck = new List<Card>();

        foreach (ushort cardId in cardIds)
        {
            // A kliens a saját Resources mappájából tölti be a kártya assetet
            // A fájlnevet a cardId alapján kell létrehozni, pl. "Cards/1"
            Card card = Resources.Load<Card>($"Cards/{cardId}");
            if (card != null)
            {
                playerDeck.Add(card);
            }
        }

        // A kliens most feltölti a saját CardManagerét
        //CardManager.instance.LoadFromDeck(playerDeck);
    }
    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        // A kliens PlayerController-e itt regisztrálja magát
        //players.Add(player);
        print("registering");
        if (playerA.Value == null)
        {
            playerA.Value = player; // Beállítjuk a PlayerA referenciát
            player.transform.parent = transform;
            player.Init(gameState.players[0], true, this);
           
        }
        else
        {
            playerB.Value = player;
            player.Init(gameState.players[1], false, this);
        }
    }
    public Dictionary<int, MinionView> minionsUI;
    public void AddMinionUIDictionary(int minion, MinionView minionView) { minionsUI.Add(minion, minionView); }
    public void DeleteMinionUIDictionary(int minion) { minionsUI.Remove(minion); }
    public Card GetCardById(ushort id) 
    {
            return testDeck.deck[0];
    }



    #region Debug
    [Server]
    public void DebugBoardState()
    {
        Debug.Log("--- Tábla Állapota ---");

        // Szövetséges minionok
        Debug.Log("--- Szövetséges Minionok ---");
        for (int i = 0; i < boardAlly.Count; i++)
        {
            MinionState minion = boardAlly[i];
            Debug.Log($"Index: {i}, SequenceId: {minion.sequenceId}, Health: {minion.currentHealth}, Attack: {minion.attack}, CanAttack: {minion.canAttack}");
        }

        // Ellenséges minionok
        Debug.Log("--- Ellenséges Minionok ---");
        for (int i = 0; i < boardEnemy.Count; i++)
        {
            MinionState minion = boardEnemy[i];
            Debug.Log($"Index: {i}, SequenceId: {minion.sequenceId}, Health: {minion.currentHealth}, Attack: {minion.attack}, CanAttack: {minion.canAttack}");
        }
    }
    List<MinionLogic> minionLogics = new List<MinionLogic>();
    public void CreateMinionLogic(ushort id)
    {
        if (minionLogics.Any(m => m._sequenceId == id))
        {
            UnityEngine.Debug.LogError($"MinionLogic with sequenceId {id} already exists!");
            return;
        }
        minionLogics.Add(new MinionLogic(id));
    }
    internal MinionLogic GetMinionLogic(ushort targetId)
    {
        return minionLogics.SingleOrDefault(m => m._sequenceId == targetId);
    }
    #endregion
}


public static class LiveTargetExtensions  // statikus osztály
{
    public static Transform GetTransform(this ILiveTarget target)  // statikus függvény 'this' kulccsal
    {
        return ((Component)target).transform;
    }
}/*
  * Ötlet: animációk string alapján, prefabként vagy metódusként
📁 Effect.AnimationName = "healEffect"
Ez az Effect assetben egy mező:

csharp
Másolás
Szerkesztés
public string animation = "healEffect";
✅ 1. Megoldás: Prefab vagy Particle animációk mappából (string alapján)
🔄 Betöltés a Resources mappából:
csharp
Másolás
Szerkesztés
Dictionary<string, GameObject> animationPrefabs;

void LoadAnimations()
{
    animationPrefabs = new();
    var all = Resources.LoadAll<GameObject>("EffectAnimations");
    foreach (var anim in all)
        animationPrefabs[anim.name] = anim;
}
▶️ Animáció indítása:
csharp
Másolás
Szerkesztés
public void PlayAnimation(ILiveTarget target, Effect e)
{
    if (animationPrefabs.TryGetValue(e.animation, out var prefab))
    {
        // például instantiate a célpont pozícióján
        GameObject.Instantiate(prefab, target.GetTransform().position, Quaternion.identity);
    }
    else
    {
        Debug.LogWarning($"Animation '{e.animation}' not found!");
    }
}
🎒 Előny: könnyen bővíthető — új prefab = új animáció, kód nélkül
🧠 Hátrány: ha nem létezik a string vagy elgéped, futásidőben derül ki

✅ 2. Alternatíva: string → metódus map
Ha inkább metódusokat akarsz futtatni:

csharp
Másolás
Szerkesztés
Dictionary<string, Action<ILiveTarget>> animationMethods = new()
{
    { "healEffect", t => SpawnParticles(t, "HealEffect") },
    { "damageEffect", t => SpawnParticles(t, "DamageEffect") }
};
csharp
Másolás
Szerkesztés
void PlayAnimation(ILiveTarget target, Effect e)
{
    if (animationMethods.TryGetValue(e.animation, out var anim))
        anim.Invoke(target);
    else
        Debug.LogWarning($"No method for animation: {e.animation}");
}*/

