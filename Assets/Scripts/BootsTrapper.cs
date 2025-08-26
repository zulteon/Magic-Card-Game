using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private GameEvents gameEvents;

    void Awake()
    {
        // Egyszer l�trehozod a GameEvents p�ld�nyt
        gameEvents = new GameEvents();

        // P�ld�ul �tadod a GameManagernek
        var gm = Object.FindAnyObjectByType<GameManager>();
        gm.AddEventSystem(gameEvents);

        // Tov�bbi komponensek inicializ�l�sa, amiknek szint�n sz�ks�g�k van a GameEvents-re
    }
}
