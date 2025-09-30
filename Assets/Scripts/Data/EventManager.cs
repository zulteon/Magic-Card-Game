using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameEvent
{
    public ushort effectId;

    // Az esemény forrása (pl. ki támadott)
    public ushort sourceId;

    // Az esemény célpontjai (pl. AoE spell több targetre is mehet)
    public List<short> targetsId;

    // Opcionális érték (pl. sebzés vagy heal mennyisége)
    public int value;

    // Esemény sorrendi index (szerver adja, segít a kliensnek sorba rakni)
    public int eventIndex;

    // Debug/replay időbélyeg
    public double timestamp;

    // Konstruktor
    public GameEvent(ushort effectId, ushort sourceId, List<short> targetsId, int value, int eventIndex)
    {
        this.effectId = effectId;
        this.sourceId = sourceId;
        this.targetsId = targetsId ?? new List<short>();
        this.value = value;
        this.eventIndex = eventIndex;
        this.timestamp = Time.timeAsDouble;
    }

    public override string ToString()
    {
        string targets = (targetsId != null && targetsId.Count > 0)
            ? string.Join(",", targetsId)
            : "none";

        return $"[GameEvent] index:{eventIndex} effectid:{effectId} src:{sourceId} → targetids:[{targets}] val:{value} ts:{timestamp:F3}";
    }
}

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    private List<GameEvent> _eventBuffer = new List<GameEvent>();
    private HashSet<int> _missingEvents = new HashSet<int>();

    private int _expectedEventIndex = 0;
    private bool _isProcessing = false;

    public System.Action<GameEvent> OnEventReady;   // EffectManager felé
    public System.Action<int> OnEventMissing;       // Ha kimarad valami

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void StartEvent(GameEvent gameEvent){ 
        //EffectManager.instance.StartEvent
        print("Starting gameevent"+gameEvent.ToString());
    }
    /// <summary>
    /// Szervertől jövő új event fogadása
    /// </summary>
    public void AddEvent(GameEvent ev)
    {
        if (ev == null) return;

        // Ha túl korán érkezett, berakjuk bufferbe a helyére
        InsertEventInOrder(ev);

        // Ha ez hiányzó volt, levesszük a missing listáról
        _missingEvents.Remove(ev.eventIndex);

        // Indítsuk a feldolgozást
        if (!_isProcessing)
            StartCoroutine(ProcessEventsCoroutine());
    }

    private void InsertEventInOrder(GameEvent ev)
    {
        int i = 0;
        while (i < _eventBuffer.Count && _eventBuffer[i].eventIndex < ev.eventIndex)
            i++;

        if (i < _eventBuffer.Count && _eventBuffer[i].eventIndex == ev.eventIndex)
        {
            // duplikátum
            return;
        }

        _eventBuffer.Insert(i, ev);
    }
    bool waiting = false;
    public void EndAnimationWaiting()
    {
        waiting = false;
    }
    private IEnumerator ProcessEventsCoroutine()
    {
        _isProcessing = true;

        while (_eventBuffer.Count > 0)
        {
            var next = _eventBuffer[0];

            if (next.eventIndex == _expectedEventIndex)
            {
                _eventBuffer.RemoveAt(0);
                waiting = true;
                StartEvent(next);
                while (waiting)
                {
                    yield return null;
                }

                _expectedEventIndex++;
            }
            else if (next.eventIndex > _expectedEventIndex)
            {
                // hiányzik valami
                for (int i = _expectedEventIndex; i < next.eventIndex; i++)
                {
                    if (!_missingEvents.Contains(i))
                    {
                        _missingEvents.Add(i);
                        OnEventMissing?.Invoke(i);
                        Debug.LogWarning($"Missing event: {i}");
                    }
                }
                // Itt kell lekérni a szervertől a hiányzó eventet.
                //GameManager.instance.GetEvent(eventId);
                // várunk, hátha megérkezik
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                // túl régi event → eldobjuk
                _eventBuffer.RemoveAt(0);
            }

            yield return null;
        }
        _isProcessing = false;
    }

}
