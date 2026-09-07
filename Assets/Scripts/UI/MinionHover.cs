using UnityEngine;

public class MinionHover : MonoBehaviour
{
    private LiveMinion _liveMinion;

    private void Awake()
    {
        _liveMinion = GetComponent<LiveMinion>();
    }

    private void OnMouseEnter()
    {
        if (_liveMinion == null)
            return;

        if (MinionCardView.instance == null)
            return;

        // 1. Azonnal kirajzoljuk a minion alapadatait
        // a helyi CardManagerbõl.
        MinionCardView.instance.ShowCard(
            _liveMinion.cardId,
            _liveMinion.sequenceId,
            transform
        );

        // 2. Közben lekérjük a szervertõl
        // az aktuális MinionLogic effektjeit.
        var controller =
            GameManager.instance.GetLocalPlayerController();

        if (controller != null)
        {
            controller.RequestMinionPreview(
                _liveMinion.sequenceId
            );
        }
    }

    private void OnMouseExit()
    {
        if (_liveMinion == null)
            return;

        if (MinionCardView.instance == null)
            return;

        MinionCardView.instance.Hide(
            _liveMinion.sequenceId
        );
    }

    private void OnDisable()
    {
        // Ha a minion hover közben meghal / eltûnik,
        // ne maradjon kint a preview.
        if (_liveMinion == null)
            return;

        if (MinionCardView.instance == null)
            return;

        MinionCardView.instance.Hide(
            _liveMinion.sequenceId
        );
    }
}