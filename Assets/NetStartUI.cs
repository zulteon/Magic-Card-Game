using UnityEngine;
using FishNet.Managing;
using System.Collections;

/// <summary>
/// Kapcsolat-indító gombok. Sima MonoBehaviour, mert a jelenetbeli
/// NetworkObject-eket a FishNet a szerver indulásáig letiltja —
/// egy NetworkBehaviour-ön lévõ gomb sosem jelenne meg.
/// </summary>
public class NetStartUI : MonoBehaviour
{
    [Header("Megjelenítés")]
    [Range(0.7f, 2f)] public float scale = 1f;
    private string _address = "127.0.0.1";
    private NetworkManager _nm;
    private bool _started;

    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _hintStyle;
    private bool _stylesInitialized;

    private Texture2D _panelTexture;
    private Texture2D _titleTexture;
    private Texture2D _buttonTexture;
    private Texture2D _buttonHoverTexture;

    private static readonly Color DarkBrown   = new Color32(58, 40, 30, 255);
    private static readonly Color HeaderBrown = new Color32(105, 73, 50, 255);
    private static readonly Color PanelBeige  = new Color32(231, 213, 180, 250);
    private static readonly Color ButtonBeige = new Color32(204, 177, 132, 255);
    private static readonly Color ButtonHover = new Color32(237, 196, 158, 255);

    private void Awake()
    {
        _nm = FindObjectOfType<NetworkManager>();
    }
    void Start()
    {
        StartCoroutine(TurnOff());
    }

    private void OnGUI()
    {
        if (_started || _nm == null) return;

        InitializeStyles();

        float w = 420f * scale;
        float h = 400f * scale;   // egy kicsit magasabb, hogy elférjen a mezõ
        Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        GUI.Box(panel, GUIContent.none, _panelStyle);

        float pad = 28f * scale;
        Rect inner = new Rect(panel.x + pad, panel.y + pad, panel.width - pad * 2f, panel.height - pad * 2f);

        GUILayout.BeginArea(inner);
        try
        {
            GUILayout.Label("Budapest \n Dzsungele", _titleStyle, GUILayout.Height(85f * scale));
            GUILayout.Space(20f * scale);

            if (GUILayout.Button("HOST", _buttonStyle, GUILayout.Height(76f * scale)))
            {
                _nm.ServerManager.StartConnection();
                _nm.ClientManager.StartConnection();
                _started = true;
            }

            GUILayout.Space(14f * scale);

            // IP mezõ a JOIN fölé
            _address = GUILayout.TextField(_address, GUILayout.Height(40f * scale));
            GUILayout.Space(8f * scale);

            if (GUILayout.Button("JOIN", _buttonStyle, GUILayout.Height(76f * scale)))
            {
                _nm.TransportManager.Transport.SetClientAddress(_address);
                _nm.ClientManager.StartConnection();
                _started = true;
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("HOST: szerver + kliens     •     JOIN: csatlakozás",
                _hintStyle, GUILayout.Height(30f * scale));
        }
        finally
        {
            GUILayout.EndArea();
        }
    }
    private void InitializeStyles()
    {
        if (_stylesInitialized) return;

        _panelTexture       = MakeTexture(PanelBeige);
        _titleTexture       = MakeTexture(HeaderBrown);
        _buttonTexture      = MakeTexture(ButtonBeige);
        _buttonHoverTexture = MakeTexture(ButtonHover);

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = _panelTexture },
            padding = new RectOffset(20, 20, 20, 20)
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            normal    = { background = _titleTexture, textColor = Color.white },
            fontSize  = Mathf.RoundToInt(30f * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding   = new RectOffset(16, 16, 12, 12)
        };

        _buttonStyle = new GUIStyle(GUI.skin.box)
        {
            normal    = { background = _buttonTexture,      textColor = DarkBrown },
            hover     = { background = _buttonHoverTexture, textColor = DarkBrown },
            active    = { background = _buttonHoverTexture, textColor = DarkBrown },
            fontSize  = Mathf.RoundToInt(34f * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            margin    = new RectOffset(4, 4, 4, 4)
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            normal    = { textColor = DarkBrown },
            fontSize  = Mathf.RoundToInt(16f * scale),
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter
        };

        _stylesInitialized = true;
    }

    private Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1)
        {
            name      = "NetStartUITexture",
            hideFlags = HideFlags.HideAndDontSave
        };
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void OnValidate()
    {
        _stylesInitialized = false;
    }

    private void OnDestroy()
    {
        DestroyTexture(_panelTexture);
        DestroyTexture(_titleTexture);
        DestroyTexture(_buttonTexture);
        DestroyTexture(_buttonHoverTexture);
    }

    private void DestroyTexture(Texture2D tex)
    {
        if (tex == null) return;
        if (Application.isPlaying) Destroy(tex);
        else DestroyImmediate(tex);
    }
    IEnumerator TurnOff()
    {
        yield return null;
        float t = 0.1f;
        while (GameManager.instance==null)
        {
            yield return null;
            t += Time.deltaTime;
        }
        if (GameManager.instance.offlineTestMode)
            Destroy(this);
    }
}