using UnityEngine;
using FishNet.Managing;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Kapcsolat-indító gombok. Sima MonoBehaviour, mert a jelenetbeli
/// NetworkObject-eket a FishNet a szerver indulásáig letiltja —
/// egy NetworkBehaviour-ön lévő gomb sosem jelenne meg.
/// </summary>
public class NetStartUI : MonoBehaviour
{
    public static NetStartUI instance;
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
    // =========================================================
    // DECK SELECT
    // =========================================================

    private List<string> _decks = new List<string>();

    private int _selectedDeckIndex = -1;
    private bool _deckDropdownOpen;

    private Vector2 _deckScroll;


    // Stílusok
    private GUIStyle _deckLabelStyle;
    private GUIStyle _dropdownStyle;
    private GUIStyle _dropdownOptionStyle;


    // Textúrák
    private Texture2D _dropdownTexture;
    private Texture2D _dropdownHoverTexture;


    private static readonly Color DropdownBeige =
        new Color32(218, 195, 154, 255);

    private static readonly Color DropdownHover =
        new Color32(242, 215, 175, 255);
    private void OnEnable()
    {
        RefreshDecks();
    }
    private void Awake()
    {
        _nm = FindObjectOfType<NetworkManager>();
        instance = this;
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
        float h = 480f * scale;

        if (_deckDropdownOpen)
            h += 120f * scale;   // egy kicsit magasabb, hogy elférjen a mező
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
                if (!PrepareSelectedDeck())
                    return;

                _nm.ServerManager.StartConnection();
                _nm.ClientManager.StartConnection();
                _started = true;
            }

            GUILayout.Space(14f * scale);

            // IP mező a JOIN fölé
            _address = GUILayout.TextField(_address, GUILayout.Height(40f * scale));
            GUILayout.Space(8f * scale);

            if (GUILayout.Button("JOIN", _buttonStyle, GUILayout.Height(76f * scale)))
            {
                if (!PrepareSelectedDeck())
                    return;

                _nm.TransportManager.Transport.SetClientAddress(_address);
                _nm.ClientManager.StartConnection();
                _started = true;
            }

            GUILayout.FlexibleSpace();
            GUILayout.Space(12f * scale);

            GUILayout.Label(
                "PAKLI",
                _deckLabelStyle,
                GUILayout.Height(24f * scale)
            );


            string selectedDeck = GetSelectedDeckName();

            string deckText =
                selectedDeck ??
                "NINCS MENTETT PAKLI";


            if (GUILayout.Button(
                deckText + "   ▼",
                _dropdownStyle,
                GUILayout.Height(42f * scale)))
            {
                if (_decks.Count > 0)
                    _deckDropdownOpen = !_deckDropdownOpen;
            }


            if (_deckDropdownOpen)
            {
                _deckScroll = GUILayout.BeginScrollView(
                    _deckScroll,
                    false,
                    true,
                    GUILayout.Height(120f * scale)
                );

                for (int i = 0; i < _decks.Count; i++)
                {
                    string text =
                        i == _selectedDeckIndex
                            ? "• " + _decks[i]
                            : "  " + _decks[i];


                    if (GUILayout.Button(
                        text,
                        _dropdownOptionStyle,
                        GUILayout.Height(34f * scale)))
                    {
                        _selectedDeckIndex = i;
                        _deckDropdownOpen = false;
                    }
                }

                GUILayout.EndScrollView();
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
#region Methods 
    private void InitializeStyles()
    {
        if (_stylesInitialized) return;
        _dropdownTexture     =MakeTexture(DropdownBeige);

        _dropdownHoverTexture=MakeTexture(DropdownHover);
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
        _deckLabelStyle =
    new GUIStyle(GUI.skin.label)
    {
        normal =
        {
            textColor = DarkBrown
        },

        fontSize =
            Mathf.RoundToInt(16f * scale),

        fontStyle =
            FontStyle.Bold,

        alignment =
            TextAnchor.MiddleLeft
    };


        _dropdownStyle =
            new GUIStyle(GUI.skin.box)
            {
                normal =
                {
            background = _dropdownTexture,
            textColor = DarkBrown
                },

                hover =
                {
            background = _dropdownHoverTexture,
            textColor = DarkBrown
                },

                active =
                {
            background = _dropdownHoverTexture,
            textColor = DarkBrown
                },

                fontSize =
                    Mathf.RoundToInt(18f * scale),

                fontStyle =
                    FontStyle.Bold,

                alignment =
                    TextAnchor.MiddleCenter,

                padding =
                    new RectOffset(10, 10, 4, 4)
            };


        _dropdownOptionStyle =
            new GUIStyle(GUI.skin.box)
            {
                normal =
                {
            background = _panelTexture,
            textColor = DarkBrown
                },

                hover =
                {
            background = _dropdownHoverTexture,
            textColor = DarkBrown
                },

                active =
                {
            background = _buttonHoverTexture,
            textColor = DarkBrown
                },

                fontSize =
                    Mathf.RoundToInt(17f * scale),

                alignment =
                    TextAnchor.MiddleLeft,

                padding =
                    new RectOffset(14, 8, 3, 3)
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
    private void RefreshDecks()
    {
        string previousDeck = GetSelectedDeckName();

        _decks = DeckStorage.ListDecks();

        _selectedDeckIndex = -1;

        // Ha az előzőleg kiválasztott deck még létezik,
        // maradjon kiválasztva.
        if (!string.IsNullOrEmpty(previousDeck))
        {
            for (int i = 0; i < _decks.Count; i++)
            {
                if (_decks[i] == previousDeck)
                {
                    _selectedDeckIndex = i;
                    break;
                }
            }
        }

        // Ha nincs korábbi választás, az első legyen az alap.
        if (_selectedDeckIndex < 0 && _decks.Count > 0)
            _selectedDeckIndex = 0;

        _deckDropdownOpen = false;
    }


    private string GetSelectedDeckName()
    {
        if (_selectedDeckIndex < 0 ||
            _selectedDeckIndex >= _decks.Count)
            return null;

        return _decks[_selectedDeckIndex];
    }


    private bool PrepareSelectedDeck()
    {
        string deckName = GetSelectedDeckName();

        if (string.IsNullOrEmpty(deckName))
        {
            Debug.LogWarning("Nincs kiválasztott pakli!");
            return false;
        }

        if (!DeckStorage.PrepareForNetwork(deckName))
        {
            Debug.LogWarning(
                $"Nem sikerült előkészíteni a paklit: {deckName}"
            );

            return false;
        }

        Debug.Log(
            $"Játékhoz előkészített pakli: {deckName}"
        );

        return true;
    }
#endregion
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
        DestroyTexture(_dropdownTexture);
        DestroyTexture(_dropdownHoverTexture);
    }

    private void DestroyTexture(Texture2D tex)
    {
        if (tex == null) return;
        if (Application.isPlaying) Destroy(tex);
        else DestroyImmediate(tex);
    }
    public void TurnOn(bool b =true)
    {
        gameObject.SetActive(b);
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