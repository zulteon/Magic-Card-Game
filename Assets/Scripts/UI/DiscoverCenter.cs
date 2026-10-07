using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class DiscoverCenter : MonoBehaviour
{
    public static DiscoverCenter instance;
    [SerializeField] private GameObject cardViewPrefab;
    [SerializeField] private Transform[] slots;

    private System.Action<ushort> _onChosen;
    private readonly List<GameObject> _spawned = new();
    public ushort cardId;
    public ushort sequenceId;   // ÚJ
    public System.Action<ushort> onChosen;

    private void OnMouseUp() => onChosen?.Invoke(sequenceId);
    private void Awake() => instance = this;

    public void Show(ushort[] cardIds, System.Action<ushort> onChosen)
    {
        _onChosen = onChosen;
        gameObject.SetActive(true);

        for (int i = 0; i < cardIds.Length; i++)
        {
            var go = Instantiate(cardViewPrefab, slots[i]);
            var view = go.GetComponent<DiscoverCardView>();
            view.SetCard(CardManager.instance.GetCard(cardIds[i]));
            view.cardId = cardIds[i];
            view.onChosen = Choose;
            _spawned.Add(go);
        }
    }

    private void Choose(ushort cardId)
    {
        var callback = _onChosen;
        Hide();
        callback?.Invoke(cardId);
    }
    private readonly HashSet<ushort> _selected = new();   // sequenceId-kat tárol

    private void Toggle(ushort sequenceId)
    {
        if (_selected.Contains(sequenceId))
            _selected.Remove(sequenceId);
        else
            _selected.Add(sequenceId);

        foreach (var go in _spawned)
        {
            var view = go.GetComponent<DiscoverCardView>();
            bool isSelected = _selected.Contains(view.sequenceId);
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.color = isSelected ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
        }
    }
    private CardState[] GetHandCards(PlayerController pc)
    {
        return pc.hand.ToArray();
    }

    private void Hide()
    {
        foreach (var go in _spawned) Destroy(go);
        _spawned.Clear();
        gameObject.SetActive(false);
    }
}