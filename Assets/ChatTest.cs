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
        // UI esem�nyek be�ll�t�sa
        sendButton.onClick.AddListener(SendMessage);
        inputField.onSubmit.AddListener(delegate { SendMessage(); });
        
    }

    private void SendMessage()
    {
        string message = inputField.text.Trim();

        if (string.IsNullOrEmpty(message))
            return;

        // �zenet k�ld�se a szervernek
        SendChatMessageServerRpc(message);

        // Input field t�rl�se �s f�kusz vissza�ll�t�sa
        inputField.text = "";
        inputField.ActivateInputField();
    }
    public override void OnStartClient()
    {
        Debug.Log("Kliens oldalon aktiv�l�dott ChatTest!");
    }
    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(string message, NetworkConnection conn = null)
    {
        // A kliens neve a NetworkConnection alapj�n
        string playerName = $"Player{conn.ClientId}";

        // Broadcast az �sszes kliensnek
        DisplayMessageObserversRpc(playerName, message);
    }

    [ObserversRpc]
    private void DisplayMessageObserversRpc(string playerName, string message)
    {
        // �zenet hozz�ad�sa a chat loghoz
        chatLog += $"{playerName}: {message}\n";

        // UI friss�t�se
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
        // Send button esem�ny
        sendButton.onClick.AddListener(SendMessage);

        // Enter lenyom�sra is k�ldje el
        inputField.onSubmit.AddListener(delegate { SendMessage(); });
    }

    void SendMessage()
    {
        string message = inputField.text.Trim();

        if (string.IsNullOrEmpty(message))
            return;

        // �zenet k�ld�se a szervernek
        SendChatMessageServerRpc(message);

        // Input field t�rl�se
        inputField.text = "";
        inputField.ActivateInputField(); // F�kusz vissza
    }

    [ServerRpc(RequireOwnership = false)]
    void SendChatMessageServerRpc(string message)
    {
        // Egyszer�s�tett verzi� - m�k�dnie kell
        string playerName = $"Player{OwnerId}";

        Debug.Log($"Szerver kapta: {playerName}: {message}");

        // Visszak�ldi mindenkinek (broadcast)
        DisplayMessageObserversRpc(playerName, message);
    }

    [ObserversRpc]
    void DisplayMessageObserversRpc(string playerName, string message)
    {
        // �zenet hozz�ad�sa a chat loghoz
        chatLog += $"{playerName}: {message}\n";

        // UI friss�t�se
        outputText.text = chatLog;

        Debug.Log($"Kliens kapta: {playerName}: {message}");
    }

    // Host ind�t�sa (szerver + kliens egyben)
    public void StartHost()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
            Debug.Log("Host elind�tva!");
        }
    }

    // Kliens csatlakoz�s
    public void StartClient()
    {
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            networkManager.ClientManager.StartConnection("127.0.0.1", 7777);
            Debug.Log("Kliens csatlakoz�s...");
        }
    }

    // Kapcsolat bont�sa
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

    // Connection st�tusz kijelz�s
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