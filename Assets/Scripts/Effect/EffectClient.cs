using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using System;
using System.Linq;
public class EffectClient : NetworkBehaviour
{
    public static EffectClient instance;

    private Queue<ClientEvent> _visualQueue = new Queue<ClientEvent>(16);
    private bool _isPlaying = false;

    [SerializeField] private float delayBetweenEffects = 0.1f; // Inspector-ból állítható

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        StopAllCoroutines();
        _queue.Clear();
        _isPlaying = false;
    }
    

    [Client]
    public void AddEvent(ClientEvent _event) //elavult
    {
        print("adding event" + _event.effectType.ToString());   
        _visualQueue.Enqueue(_event);

        if (!_isPlaying)
        {
            StartCoroutine(ProcessQueue()); 
        }
    }
    #region turn start&end
    public event Action OnTurnStart;
    public event Action OnTurnEnd;

    [Client]
    public void RaiseTurnStart()
    {
        OnTurnStart?.Invoke();
    }

    [Client]
    public void RaiseTurnEnd()
    {
        OnTurnEnd?.Invoke();
    }
    [ObserversRpc]
    public void TurnEndObserversRpc()
    {
        OnTurnEnd?.Invoke();
    }

    [ObserversRpc]
    public void TurnStartObserversRpc()
    {
        OnTurnStart?.Invoke();
    }
    #endregion
    private IEnumerator HandleReturnToHandVisual(ClientEvent e)
    {
        foreach (var id in e.targetIds)
            BoardManager.instance.ReturnMinionToHand(id);
        yield return null;
    }
    private IEnumerator ProcessQueue()
    {
        _isPlaying = true;
        while (_queue.Count > 0)
        {
            var batch = _queue.Dequeue();
            yield return StartCoroutine(HandleBatch(batch));
        }
        _isPlaying = false;
    }

    private IEnumerator PlayVisualEffect(ClientEvent e)
    {
        switch ((Effect.Type)e.effectType)
        {
            case Effect.Type.damage:
                yield return HandleDamageVisual(e);
                break;
            case Effect.Type.heal:
                yield return HandleHealVisual(e);
                break;
            case Effect.Type.death:
                yield return HandleDeathVisual(e);
                break;
            case Effect.Type.attack:
                yield return HandleAttackVisual(e);
                break;
            case Effect.Type.buff:
                yield return HandleBuffVisual(e);
                break;
            case Effect.Type.summon:
                    yield return HandleSummonVisual(e);
                    break;
            case Effect.Type.doubleStats:
                yield return HandleBuffVisual(e);//HandleDoubleStatsVisual(e);
                break;

            case Effect.Type.returnToHand:
                    yield return HandleReturnToHandVisual(e); break;
            case Effect.Type.sendToFuture:
                    yield return HandleSendToFutureVisual(e);break;
            case Effect.Type.minionSwap:
                yield return HandleMinionSwapVisual(e); break;
            case Effect.Type.setManaCrystal:
                yield return SetManaCrystal(e);break;
            case Effect.Type.playCard:
                try
                {

                    
                    CardInRealAction.instance.ShowCard(e.targetIds[0], e.targetIds.Length > 1 ? e.targetIds[1]:0);
                }
                catch { }
                break;
            case Effect.Type.silence:
                HandleSilenceVisual(e); break;
            case Effect.Type.setStats:
                yield return HandleSetStats(e); break;
            case Effect.Type.steal:
               yield return HandleStealVisual(e); break;
            case Effect.Type.copyStats:
                yield return HandleCopyStatsVisual(e);break;
            case Effect.Type.debuff:
                yield return HandleDebuffVisual(e); break;
            case Effect.Type.swapAttackHealth:
                yield return HandleSwapAttackAndHealthVisual(e); break;
            
            default:
                Debug.LogWarning($"Unknown effect type: {e.effectType}");
                yield break;
        }
    }


    #region BatchProcess
    private readonly Queue<ClientEvent[]> _queue = new();

    public void AddEventBatch(ClientEvent[] batch)
    {
        print($"New event received: {(Effect.Type)batch[0].effectType}");
        if (batch == null || batch.Length == 0) return;
        _queue.Enqueue(batch);
        if (!_isPlaying) StartCoroutine(ProcessQueue());
    }
    private IEnumerator HandleBatch(ClientEvent[] batch)
    {
        var running = new List<Coroutine>();

        foreach (var e in batch)
        {
            Coroutine c = TryStartVisualEffect(e);

            if (c != null)
                running.Add(c);
        }

        foreach (var c in running)
            yield return c;
    }
    private Coroutine TryStartVisualEffect(ClientEvent e)
    {
        try
        {
            return StartCoroutine(PlayVisualEffect(e));
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[EffectClient] {(Effect.Type)e.effectType} visual failed:\n{ex}"
            );

            return null;
        }
    }

    #endregion
    private IEnumerator HandleCopyStatsVisual(ClientEvent e)
    {
        if (e.newValues == null || e.newValues.Length < 2)
        {
            Debug.LogWarning("CopyStats ClientEvent newValues invalid.");
            yield break;
        }

        MinionView doerView =
            GameManager.instance.GetMinionView(e.doerId);

        if (doerView == null)
            yield break;

        int newAttack = e.newValues[0];
        int newHealth = e.newValues[1];

        yield return StartCoroutine(
            doerView.PlayBuffAnimation(
                newAttack,
                newHealth,
                e.value
            )
        );
    }
    private IEnumerator HandleSwapAttackAndHealthVisual(ClientEvent e)
    {
        if (e.targetIds == null || e.targetIds.Length == 0)
            yield break;

        if (e.newValues == null ||
            e.newValues.Length < e.targetIds.Length * 2)
        {
            Debug.LogWarning("[SwapAttackAndHealth] Invalid newValues.");
            yield break;
        }

        for (int i = 0; i < e.targetIds.Length; i++)
        {
            MinionView view =
                GameManager.instance.GetMinionView(e.targetIds[i]);

            if (view == null)
                continue;

            int valueIndex = i * 2;

            int newAttack = e.newValues[valueIndex];
            int newHealth = e.newValues[valueIndex + 1];

            view.UpdateAttackVisual(newAttack);
            view.UpdateHealthVisual(newHealth);
        }

        yield return null;
    }
    private IEnumerator HandleDoubleStatsVisual(ClientEvent e)
    {
        for (int i = 0; i < e.targetIds.Length; i++)
        {
            MinionView view = GameManager.instance.GetMinionView(e.targetIds[i]);

            int valueIndex = i * 2;
            if (e.newValues == null || e.newValues.Length <= valueIndex + 1)
            {
                Debug.LogWarning("DoubleStats ClientEvent newValues is invalid.");
                continue;
            }

            int newAttack = e.newValues[valueIndex];
            int newHealth = e.newValues[valueIndex + 1];

            if (view == null) continue;

            if (i == e.targetIds.Length - 1)
                yield return StartCoroutine(view.PlayBuffAnimation(newAttack, newHealth, e.value));
            else
            {
                StartCoroutine(view.PlayBuffAnimation(newAttack, newHealth, e.value));
               // yield return new WaitForSeconds(0.1f);
            }
        }
    }
    private void HandleSilenceVisual(ClientEvent e)
    {
        ushort id = (ushort)e.targetIds[0];
        var lm = BoardManager.instance.GetLiveMinion(id);
        if (lm == null) return;

        var view = lm.GetComponent<MinionView>();
        if (view == null) return;

        // statok frissítése
        view.UpdateAttackVisual(e.newValues[0]);
        view.UpdateHealthVisual(e.newValues[1]);

        // taunt UI törlése
        view.TauntUI(false);

    }

    private IEnumerator SetManaCrystal(ClientEvent e)
    {
        if (e.targetIds == null || e.targetIds.Length == 0) yield break;
        ushort home = GameManager.instance.AreWeHomePlayer() ? (ushort)0 : (ushort)1;
        if (home != e.targetIds[0])
        {
            EnemyMana.instance.Fill(e.value);
            yield break;
        }//yield break;
        else
        {
            print("Seeting mana");
            ManaCenterUI.instance.SetMana(e.value);
            yield return null;
        }
    }
    private IEnumerator HandleMinionSwapVisual(ClientEvent e)
    {
        yield return BoardManager.instance.AnimateArcTo(e.targetIds[0], e.value);
    }
    // EffectClient
    private IEnumerator HandleSendToFutureVisual(ClientEvent e)
    {
        foreach (var id in e.targetIds)
            BoardManager.instance.ReturnMinionToHand(id);   // ugyanaz az animáció újrahasznosítva, vagy egyedi "eltűnés" animáció
        yield return null;
    }
    private IEnumerator HandleBuffVisual(ClientEvent e)
    {
        var running = new List<Coroutine>();

        for (int i = 0; i < e.targetIds.Length; i++)
        {
            int valueIndex = i * 2;

            if (e.newValues == null || e.newValues.Length <= valueIndex + 1)
            {
                Debug.LogWarning("Buff ClientEvent newValues is invalid.");
                continue;
            }

            int newAttack = e.newValues[valueIndex];
            int newHealth = e.newValues[valueIndex + 1];

            MinionView view = GameManager.instance.GetMinionView(e.targetIds[i]);

            if (view == null)
            {
                var cardView = GameManager.instance.GetPlayer().showHand
                    .FindCardView(e.targetIds[i]);
                cardView?.PlayBuffFlash(newAttack, newHealth);
                continue;
            }

            Vector2Int oldStats = view.GetStats();
            if (oldStats.x == newAttack && oldStats.y == newHealth) continue;

            running.Add(StartCoroutine(
                view.PlayBuffAnimation(newAttack, newHealth, e.value)));
        }

        foreach (var c in running)
            yield return c;
    }
    private IEnumerator HandleDebuffVisual(ClientEvent e)
    {
        var running = new List<Coroutine>();

        for (int i = 0; i < e.targetIds.Length; i++)
        {
            int valueIndex = i * 2;

            if (e.newValues == null ||
                e.newValues.Length <= valueIndex + 1)
            {
                Debug.LogWarning(
                    $"Debuff ClientEvent newValues invalid. targetIndex={i}"
                );

                continue;
            }

            int newAttack =
                e.newValues[valueIndex];

            int newHealth =
                e.newValues[valueIndex + 1];


            MinionView view =
                GameManager.instance.GetMinionView(
                    e.targetIds[i]
                );

            if (view == null)
                continue;


            Vector2Int oldStats =
                view.GetStats();

            if (oldStats.x == newAttack &&
                oldStats.y == newHealth)
            {
                continue;
            }


            running.Add(
                StartCoroutine(
                    view.PlayBuffAnimation(
                        newAttack,
                        newHealth,
                        -Mathf.Abs(e.value)
                    )
                )
            );
        }


        foreach (Coroutine coroutine in running)
            yield return coroutine;
    }
    private IEnumerator HandleAttackVisual(ClientEvent e)
    {
        yield return CombatHandler.instance.Attack(e.doerId, e.targetIds[0], e.newValues[0], e.newValues[1]);
    }
    private IEnumerator HandleDamageVisual(ClientEvent e)
    {
        for (int i = 0; i < e.targetIds.Length; i++)
        {
            // ✅ JAVÍTVA: GameManager.instance
            MinionView view = GameManager.instance.GetMinionView(e.targetIds[i]);
            
            if (view != null)
            {
                view.PlayDamageAnimation(e.value);
                view.UpdateHealthVisual(e.newValues[i]);
            }
        }
        yield return new WaitForSeconds(0.5f);
    }
    private IEnumerator HandleSummonVisual(ClientEvent e)
    {
        bool ownerIsAlly =
            e.newValues[0] == 1;

        bool isHome =
            ownerIsAlly == GameManager.instance.AreWeHomePlayer();
        ushort id =
            e.targetIds[0];

        ushort cardId = (ushort)e.newValues[1];

        short attack = (short)e.newValues[2];

        ushort health = (ushort)e.newValues[3];
        bool hasTaunt=e.newValues[4] == 1;

        MinionState state = new MinionState
        {
            sequenceId = id,
            cardId = cardId,
            attack= attack,
            currentHealth = health,
            taunt=hasTaunt,
        };


        BoardManager.instance.SpawnMinion(
            state,
            isHome
        );

        yield return null;
    }
    private IEnumerator HandleStealVisual(ClientEvent e)
    {
        if (e.targetIds == null || e.targetIds.Length == 0)
            yield break;

        if (e.newValues == null || e.newValues.Length < 4)
            yield break;

        MinionView doerView =
            GameManager.instance.GetMinionView(e.doerId);

        MinionView targetView =
            GameManager.instance.GetMinionView(e.targetIds[0]);


        int doerAttack = e.newValues[0];
        int doerHealth = e.newValues[1];

        int targetAttack = e.newValues[2];
        int targetHealth = e.newValues[3];


        // ==========================================
        // 1. ELLOPJA A TARGETTŐL
        // ==========================================

        if (targetView != null)
        {
            yield return StartCoroutine(
                targetView.PlayBuffAnimation(
                    targetAttack,
                    targetHealth,
                    -e.value
                )
            );
        }


        // ==========================================
        // 2. UTÁNA MEGSZERZI
        // ==========================================

        if (doerView != null)
        {
            yield return StartCoroutine(
                doerView.PlayBuffAnimation(
                    doerAttack,
                    doerHealth,
                    e.value
                )
            );
        }
    }
    private IEnumerator HandleSetStats(ClientEvent e)
    {
        // ha több target is van van foreachet beépiteni 
        if (e.targetIds == null || e.targetIds.Length == 0) yield break;
        var lm = BoardManager.instance.GetLiveMinion(e.targetIds[0]);
        if (lm == null) yield break;

        var view = lm.GetComponent<MinionView>();
        if (view == null) yield break;
        if (e.newValues[0]>0)
            view.UpdateAttackVisual(e.newValues[0]);
        if (e.newValues[1] > 0)
            view.UpdateHealthVisual(e.newValues[1]);


        yield break;
    }
    private IEnumerator HandleHealVisual(ClientEvent e)
    {
        for (int i = 0; i < e.targetIds.Length; i++)
        {
            var lm =
                BoardManager.instance.GetLiveMinion(
                    e.targetIds[i]
                );

            if (lm == null)
                continue;

            var view =
                lm.GetComponent<MinionView>();

            if (view == null)
                continue;

            int valueIndex = i * 2;

            int newHealth =
                e.newValues[valueIndex + 1];

            view.UpdateHealthVisual(newHealth);
            view.PlayHealAnimation(newHealth);
        }

        yield break;
    }
    private IEnumerator HandleAddTriggerVisual(ClientEvent e)
    {
        yield return
        // TODO: Trigger effekt vizualizáció
         new WaitForSeconds(0.3f);
    }

    private IEnumerator HandleDeathVisual(ClientEvent e)
    {
        foreach (var id in e.targetIds)
            BoardManager.instance.DestroyMinion(id);
        yield return null;
    }

    public List<MinionView> getTargets(ClientEvent e)
    {
        List<MinionView> minions=new List<MinionView>();
        foreach(ushort i in e.targetIds)
        {
            
        }return minions;
    }
    
}
/* Effect COntextbe van iderakom a könnyü olvasásért 
/*[System.Serializable]
public struct ClientEvent
{
    public ushort effectType;
    public ushort[] targetIds; // Tömb, mert így több célpontot is lefedhet egyetlen esemény
    public int value;
    public ushort doerId;
}*/