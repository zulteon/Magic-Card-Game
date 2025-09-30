// A "robusztus, minimalista" MinionView
using TMPro;
using UnityEngine;

public class MinionView : MonoBehaviour
{
    // Caching a referenci�kat a "minimalista" elv szerint
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    LiveMinion _liveMinion;
    // A view fogadja az adatokat, nem k�ri le �ket!
    public void Initialize(string sprite, short attack, ushort health)
    {
        // Minimalista megold�s: a view megkapja az adatokat
        // a GameManager vagy egy m�sik View/Controller oszt�lyt�l.
       // spriteRenderer.sprite = ImageManager.GetImage(sprite);
        attackText.text = attack.ToString();
        healthText.text = health.ToString();
        _liveMinion=GetComponent<LiveMinion>();
    }

    public void UpdateStats(short attack, ushort health)
    {
        // A view csak azt a feladatot l�tja el, ami a neve: a l�tv�ny friss�t�se.
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