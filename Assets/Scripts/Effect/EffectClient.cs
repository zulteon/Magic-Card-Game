using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;

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
        _visualQueue.Clear();
        _isPlaying = false;
    }

    [Client]
    public void AddEvent(ClientEvent _event)
    {
        print("adding event" + _event.effectType);
        _visualQueue.Enqueue(_event);

        if (!_isPlaying)
        {
            StartCoroutine(ProcessQueue()); 
        }
    }
    private IEnumerator ProcessQueue()
    {
        _isPlaying = true;

        while (_visualQueue.Count > 0)
        {
            ClientEvent currentEvent = _visualQueue.Dequeue();

            // Opcionális: validálás
            if (currentEvent.targetIds == null || currentEvent.targetIds.Length == 0)
            {
                Debug.LogWarning($"Invalid event skipped: {currentEvent.effectType}");
                continue; // Skip és következő
            }

            yield return StartCoroutine(PlayVisualEffect(currentEvent));

            if (_visualQueue.Count > 0)
            {
                yield return new WaitForSeconds(delayBetweenEffects);
            }
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
            default:
                Debug.LogWarning($"Unknown effect type: {e.effectType}");
                yield break;
        }
    }
    private IEnumerator HandleBuffVisual(ClientEvent e)
    {
        for (int i = 0; i < e.targetIds.Length; i++)
        {
            MinionView view =
                GameManager.instance.GetMinionView(e.targetIds[i]);

            // ── 1. Előbb az értékek, MÉG a null-vizsgálat előtt ──
            int valueIndex = i * 2;

            if (e.newValues == null ||
                e.newValues.Length <= valueIndex + 1)
            {
                Debug.LogWarning("Buff ClientEvent newValues is invalid.");
                continue;
            }

            int newAttack = e.newValues[valueIndex];
            int newHealth = e.newValues[valueIndex + 1];

            // ── 2. Nincs MinionView → a célpont a KÉZBEN van ──
            if (view == null)
            {
                var cardView = GameManager.instance
                    .GetPlayer()
                    .showHand
                    .FindCardView(e.targetIds[i]);

                if (cardView != null)
                    cardView.PlayBuffFlash(newAttack, newHealth);

                continue;
            }

            // ── 3. Pályán lévő lény: a meglévő logika ──
            if (i == e.targetIds.Length - 1)
            {
                yield return StartCoroutine(
                    view.PlayBuffAnimation(
                        newAttack,
                        newHealth,
                        e.value
                    )
                );
            }
            else
            {
                StartCoroutine(
                    view.PlayBuffAnimation(
                        newAttack,
                        newHealth,
                        e.value
                    )
                );

                yield return new WaitForSeconds(0.1f);
            }
        }

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
    {yield return
        // TODO: Halál animáció
        new WaitForSeconds(0.8f);
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