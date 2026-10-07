using System.Collections;
using UnityEngine;
public class SceneManagement : MonoBehaviour
{
    public static SceneManagement instance;

    [SerializeField] private GameObject deckViewer;

    
    public bool skipDeckView;

    private void Awake()
    {
        instance = this;
    }

    private IEnumerator Start()
    {
        while (GameManager.instance == null)
            yield return null;

        bool showDeckViewer =
            !skipDeckView &&
            !GameManager.instance.offlineTestMode;

        SetDeckViewer(showDeckViewer);
    }

    public void OpenMainMenu()
    {
        SetDeckViewer(false);
    }

    public void OpenDeckViewer()
    {
        SetDeckViewer(true);
    }

    private void SetDeckViewer(bool show)
    {
        deckViewer.SetActive(show);
        NetStartUI.instance.TurnOn(!show);
    }
}