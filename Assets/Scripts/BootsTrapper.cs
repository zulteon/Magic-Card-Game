using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private GameEvents gameEvents;

    void Awake()
    {
        // Egyszer létrehozod a GameEvents példányt
        gameEvents = new GameEvents();

        // Például átadod a GameManagernek
        var gm = Object.FindAnyObjectByType<GameManager>();
        gm.AddEventSystem(gameEvents);

        // További komponensek inicializálása, amiknek szintén szükségük van a GameEvents-re
    }
}
