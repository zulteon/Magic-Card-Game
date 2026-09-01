using UnityEngine;

public class WinLoose_Show : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static WinLoose_Show Instance;
    public Transform win;
    public Transform loose;
    public Transform keret;
    void Start()
    {
        Instance = this;   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GameOver(bool win)
    {
        keret.gameObject.SetActive(true);
        keret.gameObject.SetActive(win);
        loose.gameObject.SetActive(!win);
    }
}
