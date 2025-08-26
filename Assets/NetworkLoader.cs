using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

public class NetworkLoader : MonoBehaviour
{
    public string sceneName = "MyNetworkScene";

    private void Start()
    {
        if (InstanceFinder.IsServer)
        {
            InstanceFinder.SceneManager.LoadGlobalScenes(new SceneLoadData(sceneName));
        }
    }
}
