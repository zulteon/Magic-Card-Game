using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using System;
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
                SetManaCrystal(e);break;
            case Effect.Type.playCard:
                try
                {
                    CardInRealAction.instance.ShowCard(e.targetIds[0]);
                }
                catch { }
                break;
            default:
                Debug.LogWarning($"Unknown effect type: {e.effectType}");
                yield break;
        }
    }


    #region BatchProcess
    private readonly Queue<ClientEvent[]> _queue = new();

    public void AddEventBatch(ClientEvent[] batch)
    {
        if (batch == null || batch.Length == 0) return;
        _queue.Enqueue(batch);
        if (!_isPlaying) StartCoroutine(ProcessQueue());
    }
    private IEnumerator HandleBatch(ClientEvent[] batch)
    {
        var running = new List<Coroutine>();

        foreach (var e in batch)
            running.Add(StartCoroutine(PlayVisualEffect(e)));

        foreach (var c in running)
            yield return c;                    // mind lefut, aztán jön a következő ütem
    }

    #endregion
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
    private void SetManaCrystal(ClientEvent e)
    {
        ManaCenterUI.instance.SetMana(e.value);
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
        if (e.newValues == null || e.newValues.Length == 0)
            yield break;

        bool ownerIsAlly = e.newValues[0] == 1;
        bool isHome = (ownerIsAlly == GameManager.instance.AreWeHomePlayer());

        foreach (var id in e.targetIds)
        {
            var state = GameManager.instance.GetMinionById(id);

            if (state.cardId == 0)
            {
                Debug.LogWarning($"[Summon] {id} már nincs a boardon, kihagyva.");
                continue;
            }

            BoardManager.instance.SpawnMinion(state, isHome);
        }

        yield return  null;
    }
    private IEnumerator HandleHealVisual(ClientEvent e)
    {
        yield return null;
      /*
        for (int i = 0; i < e.targetIds.Length; i++)
        {
            MinionView view = GameManager.instance?.GetMinionView(e.targetIds[i]);

            if (view != null)
            {
                view.PlayHealAnimation(e.value);
                view.UpdateHealthVisual(e.newValues[i]);
            }
        }
        yield return new WaitForSeconds(0.5f);*/
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