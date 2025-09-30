// A "robusztus, minimalista" MinionView
using TMPro;
using UnityEngine;

public class MinionView : MonoBehaviour
{
    // Caching a referenciákat a "minimalista" elv szerint
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    LiveMinion _liveMinion;
    // A view fogadja az adatokat, nem kéri le õket!
    public void Initialize(string sprite, short attack, ushort health)
    {
        // Minimalista megoldás: a view megkapja az adatokat
        // a GameManager vagy egy másik View/Controller osztálytól.
       // spriteRenderer.sprite = ImageManager.GetImage(sprite);
        attackText.text = attack.ToString();
        healthText.text = health.ToString();
        _liveMinion=GetComponent<LiveMinion>();
    }

    public void UpdateStats(short attack, ushort health)
    {
        // A view csak azt a feladatot látja el, ami a neve: a látvány frissítése.
        attackText.text = attack.ToString();
        healthText.text = health.ToString();
    }
    private void OnMouseDown()
    {
        if (_liveMinion != null)
        {
            _liveMinion.StartAttackClick();
        }
    }
}