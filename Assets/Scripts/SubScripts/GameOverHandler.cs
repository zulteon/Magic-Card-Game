using UnityEngine;
public class GameOverHandler : MonoBehaviour
{
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject defeatScreen;

    public void TriggerGameOver(bool playerWon)
    {
        if (playerWon)
            Instantiate(victoryScreen);
        else
            Instantiate(defeatScreen);
    }
}
