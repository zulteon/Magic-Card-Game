using FishNet.Object;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.UI;

public class ChatTest : NetworkBehaviour
{
    [Header("UI References")]
    public Text outputText;
    public InputField inputField;
    public Button sendButton;

    private string chatLog = "";

    void Start()
    {
        outputText.text = "juj";
        // UI események beállítása
        sendButton.onClick.AddListener(SendMessage);
        inputField.onSubmit.AddListener(delegate { SendMessage(); });
        
    }

    private void SendMessage()
    {
        string message = inputField.text.Trim();

        if (string.IsNullOrEmpty(message))
            return;

        // Üzenet küldése a szervernek
        SendChatMessageServerRpc(message);

        // Input field törlése és fókusz visszaállítása
        inputField.text = "";
        inputField.ActivateInputField();
    }
    public override void OnStartClient()
    {
        Debug.Log("Kliens oldalon aktiválódott ChatTest!");
    }
    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(string message, NetworkConnection conn = null)
    {
        // A kliens neve a NetworkConnection alapján
        string playerName = $"Player{conn.ClientId}";

        // Broadcast az összes kliensnek
        DisplayMessageObserversRpc(playerName, message);
    }

    [ObserversRpc]
    private void DisplayMessageObserversRpc(string playerName, string message)
    {
        // Üzenet hozzáadása a chat loghoz
        chatLog += $"{playerName}: {message}\n";

        // UI frissítése
        outputText.text = message;
    }
}
    /*
    [Header("UI References")]
    public Text outputText;
    public InputField inputField;
    public Button sendButton;

    private string chatLog = "";

    void Start()
    {
        // Send button esemény
        sendButton.onClick.AddListener(SendMessage);

        // Enter lenyomásra is küldje el
        inputField.onSubmit.AddListener(delegate { SendMessage(); });
    }

    void SendMessage()
    {
        string message = inputField.text.Trim();

        if (string.IsNullOrEmpty(message))
            return;

        // Üzenet küldése a szervernek
        SendChatMessageServerRpc(message);

        // Input field törlése
        inputField.text = "";
        inputField.ActivateInputField(); // Fókusz vissza
    }

    [ServerRpc(RequireOwnership = false)]
    void SendChatMessageServerRpc(string message)
    {
        // Egyszerûsített verzió - mûködnie kell
        string playerName = $"Player{OwnerId}";

        Debug.Log($"Szerver kapta: {playerName}: {message}");

        // Visszaküldi mindenkinek (broadcast)
        DisplayMessageObserversRpc(playerName, message);
    }

    [ObserversRpc]
    void DisplayMessageObserversRpc(string playerName, string message)
    {
        // Üzenet hozzáadása a chat loghoz
        chatLog += $"{playerName}: {message}\n";

        // UI frissítése
        outputText.text = chatLog;

        Debug.Log($"Kliens kapta: {playerName}: {message}");
    }

    // Host indítása (szerver + kliens egyben)
    public void StartHost()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
            Debug.Log("Host elindítva!");
        }
    }

    // Kliens csatlakozás
    public void StartClient()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            networkManager.ClientManager.StartConnection("127.0.0.1", 7777);
            Debug.Log("Kliens csatlakozás...");
        }
    }

    // Kapcsolat bontása
    public void Disconnect()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            if (networkManager.IsServerStarted)
                networkManager.ServerManager.StopConnection(true);

            if (networkManager.IsClientStarted)
                networkManager.ClientManager.StopConnection();

            Debug.Log("Kapcsolat bontva!");
        }
    }

    // Connection státusz kijelzés
    void OnGUI()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 100));

        if (networkManager.IsHostStarted)
            GUILayout.Label("Status: HOST");
        else if (networkManager.IsServerStarted)
            GUILayout.Label("Status: SERVER");
        else if (networkManager.IsClientStarted)
            GUILayout.Label("Status: CLIENT");
        else
            GUILayout.Label("Status: DISCONNECTED");

        GUILayout.EndArea();
    }
}*/