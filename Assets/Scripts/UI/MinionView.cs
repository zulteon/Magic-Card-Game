// A "robusztus, minimalista" MinionView
using TMPro;
using UnityEngine;

public class MinionView : MonoBehaviour
{
    // Caching a referenciákat a "minimalista" elv szerint
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // A view fogadja az adatokat, nem kéri le õket!
    public void Initialize(Sprite sprite, short attack, ushort health)
    {
        // Minimalista megoldás: a view megkapja az adatokat
        // a GameManager vagy egy másik View/Controller osztálytól.
        spriteRenderer.sprite = sprite;
        attackText.text = attack.ToString();
        healthText.text = health.ToString();
    }

    public void UpdateStats(short attack, ushort health)
    {
        // A view csak azt a feladatot látja el, ami a neve: a látvány frissítése.
        attackText.text = attack.ToString();
        healthText.text = health.ToString();
    }
}