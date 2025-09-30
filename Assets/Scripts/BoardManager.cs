using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    List<GameObject> home = new List<GameObject>(), abroad=new List<GameObject>();
    float margin = 0.5f;
    float minionsize = 1.7f;
    float YHeight = 3f;
    float minusYheight = -3f;
    Transform boardMinions;
    public GameObject minionPrefab;
    private void Awake()
    {
        boardMinions = new GameObject("BoardMinions").transform;
        boardMinions.transform.parent = transform;
        new GameObject("Home").transform.parent = boardMinions;
        new GameObject("Abroad").transform.parent = boardMinions;
    }
    void Start()
    {
        //minionPrefab = GameManager.instance.minionPrefabUI;
    }
   
    public void AddToBoard(GameObject minion,bool toHome=true)
    {
        if (minion == null) print("whyy");
        if (toHome) home.Add(minion);
        else
            abroad.Add(minion);
        Arrangecards();
    }
 
    public void OnBoardChangeHome(SyncListOperation op, int index, MinionState oldItem, MinionState newItem, bool asServer)
    {
        print($"A tábla megváltozott:  {newItem.cardId} (index {index}) op :{op.ToString()} isServer:{asServer}");
        
        if (asServer) return;
        print("Board change Home");
        if (op == SyncListOperation.Add )
        {
            print("Friend Spawned");
            home.Add(CreateMinionUI(newItem));
        }
        else if (op == SyncListOperation.RemoveAt && index < home.Count)
        {
            
            Destroy(home[index]);
            home.RemoveAt(index);
        }
        Arrangecards(); 
    }
    public void OnBoardChangeEnemy(SyncListOperation op, int index, MinionState oldItem, MinionState newItem, bool asServer)
    {
        print($"A ellenfél táblája megváltozott:  {newItem.cardId} (index {index}) op :{op.ToString()} isServer:{asServer}");
        if (asServer) return;
        if (op == SyncListOperation.Add )
        {
            print("Enemy Spawned");
            abroad.Add(CreateMinionUI(newItem,false));
        }
        else if (op == SyncListOperation.RemoveAt && index < home.Count)
        {
            Destroy(home[index]);
            home.RemoveAt(index);
        }
        Arrangecards();
    }
    public GameObject CreateMinionUI(MinionState minion,bool home=true)
    {
        print("Creating a minion UI");
        GameObject newMinion=Instantiate(minionPrefab,boardMinions.GetChild(home?0:1));
        newMinion.GetComponent<LiveMinion>().InitFromMinionState(minion);
        newMinion.GetComponent<MinionView>().Initialize(CardManager.instance.GetMinion(minion.cardId).sprite,minion.attack,minion.currentHealth);
        return newMinion;
    }
    void Arrangecards(bool home =true)
    {
        
        int count=this.home.Count;
        float size; 
        float startingPoint;
        int counter = 0;
        if (home)
        {
            size = margin * (count - 1) + minionsize * count;
            startingPoint = -size / 2 + minionsize / 2;
            foreach (GameObject i in this.home)
            {
                float pos = startingPoint + counter * (margin + minionsize);
                i.transform.position = new Vector3(pos, minusYheight, 0);
                counter++;
            }
           // return;
        }
        counter= 0; 
        count = abroad.Count;
        size = margin * (count - 1) + minionsize * count;
        startingPoint = -size / 2 + minionsize / 2;
        foreach (GameObject i in abroad)
        {
            
            float pos = startingPoint + counter * (margin + minionsize);
            i.transform.position = new Vector3(pos, YHeight, 0);
            counter++;
        }

    }
}
