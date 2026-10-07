using UnityEngine;
using System.Collections.Generic;

public class MulliganCenter : MonoBehaviour
{
    public static MulliganCenter instance;
    [SerializeField] private GameObject cardViewPrefab;
    [SerializeField] private GameObject confirmButton;
    [SerializeField] private float cardSpacing = 2.5f;  // távolság a lapok között

    private System.Action<ushort[]> _onConfirm;
    private readonly List<GameObject> _spawned = new();
    private readonly HashSet<ushort> _selected = new();

    private void Awake() => instance = this;
    float startX=0;
    // MulliganCenter.Show:
    public void Show(ushort[] cardIds, ushort[] sequenceIds, System.Action<ushort[]> onConfirm)
    {
        _onConfirm = onConfirm;
        confirmButton?.SetActive(true);
        Debug.Log($"Show fut, cardIds={cardIds.Length}, gameObject={gameObject.name}, active={gameObject.activeSelf}");
        float totalWidth = (cardIds.Length - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        for (int i = 0; i < cardIds.Length; i++)
        {
            var go = Instantiate(cardViewPrefab, transform);
            go.transform.localPosition = new Vector3(startX + i * cardSpacing, 0, 0);

            var view = go.GetComponent<DiscoverCardView>();
            view.SetCard(CardManager.instance.GetCard(cardIds[i]));
            view.cardId = cardIds[i];
            view.sequenceId = sequenceIds[i];
            view.onChosen = Toggle;
            _spawned.Add(go);
        }
    }

    private void Toggle(ushort sequenceId)
    {
        Debug.Log($"Toggle: sequenceId={sequenceId}");
        Debug.Log($"Toggle hívva: sequenceId={sequenceId}");

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

    public void Confirm()
    {
        Debug.Log($"1. Confirm eleje, PC active={GameManager.instance.GetLocalPlayerController()?.gameObject.activeSelf}");

        var callback = _onConfirm;
        var toReplace = new ushort[_selected.Count];
        _selected.CopyTo(toReplace);

        Debug.Log($"2. Hide elõtt, PC active={GameManager.instance.GetLocalPlayerController()?.gameObject.activeSelf}");
        Hide();

        Debug.Log($"3. Hide után, PC active={GameManager.instance.GetLocalPlayerController()?.gameObject.activeSelf}");
        callback?.Invoke(toReplace);

        Debug.Log($"4. Callback után, PC active={GameManager.instance.GetLocalPlayerController()?.gameObject.activeSelf}");
    }

    private void Hide()
    {
        foreach (var go in _spawned) Destroy(go);
        _spawned.Clear();
        _selected.Clear();
        gameObject.SetActive(false);
    }
}