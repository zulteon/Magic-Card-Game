using UnityEngine;

using System.Collections;
using System.Collections.Generic;
public class DiscoverCenter : MonoBehaviour
{
    public static DiscoverCenter instance;
    [SerializeField] private GameObject cardViewPrefab;
    [SerializeField] private Transform[] slots;

    private System.Action<ushort> _onChosen;
    private readonly List<GameObject> _spawned = new();

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

    private void Hide()
    {
        foreach (var go in _spawned) Destroy(go);
        _spawned.Clear();
        gameObject.SetActive(false);
    }
}