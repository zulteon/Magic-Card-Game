using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    public static SceneManagement instance;

    [SerializeField] private string deckViewerScene = "DeckViewer";

    private GameObject canvasRoot;

    private void Awake()
    {
        instance = this;
    }

    private IEnumerator Start()
    {
        while (GameManager.instance==null) yield return null;
        float t =1.8f;
        while (t > 0)
            t -= Time.deltaTime;
        if (GameManager.instance.offlineTestMode) yield break;
        Scene scene =
            SceneManager.GetSceneByName(deckViewerScene);

        if (!scene.isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(
                deckViewerScene,
                LoadSceneMode.Additive
            );
        }

        canvasRoot =
            GameObject.Find("deckViewCanvas");

        if (canvasRoot == null)
        {
            Debug.LogError(
                "deckViewCanvas nem található!"
            );

            yield break;
        }

        // Kezdéskor a DeckViewer látszik,
        // a network menü nem.
        NetStartUI.instance.TurnOn(false);

        Debug.Log(
            "DeckViewer rá lett töltve a MainScene-re."
        );
    }


    public void OpenMainMenu()
    {
        canvasRoot.SetActive(false);

        NetStartUI.instance.TurnOn(true);
    }


    public void OpenDeckViewer()
    {
        NetStartUI.instance.TurnOn(false);

        canvasRoot.SetActive(true);
    }
}