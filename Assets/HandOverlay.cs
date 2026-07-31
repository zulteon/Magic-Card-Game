using UnityEngine;

public class HandOverlay : MonoBehaviour
{
    [Header("Megjelenítés")]
    public bool show = false;
    public KeyCode toggleKey = KeyCode.F2;

    [Header("Melyik játékos kezét mutassa")]
    public bool showPlayerOne = true;

    private Vector2 _scroll;
    private GUIStyle _panelStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _cardStyle;
    private GUIStyle _bonusStyle;
    private GUIStyle _emptyStyle;
    private bool _stylesInitialized;

    private Texture2D _panelTexture;
    private Texture2D _headerTexture;
    private Texture2D _cardTexture;
    private Texture2D _bonusTexture;
    private Texture2D _emptyTexture;

    private static readonly Color DarkBrown = new Color32(58, 40, 30, 255);
    private static readonly Color HeaderBrown = new Color32(105, 73, 50, 255);
    private static readonly Color PanelBeige = new Color32(231, 213, 180, 250);
    private static readonly Color CardCream = new Color32(249, 239, 215, 255);
    private static readonly Color BonusGreen = new Color32(200, 224, 180, 255);
    private static readonly Color EmptyPeach = new Color32(237, 196, 158, 255);

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            show = !show;
    }

    private void OnGUI()
    {
        if (!show || GameManager.instance == null)
            return;

        InitializeStyles();

        float panelWidth = Screen.width * 0.8f;
        float panelHeight = Screen.height * 0.85f;
        Rect panelRect = new Rect(Screen.width - panelWidth - 20f, 20f, panelWidth, panelHeight);

        GUI.Box(panelRect, GUIContent.none, _panelStyle);

        Rect contentRect = new Rect(panelRect.x + 20f, panelRect.y + 20f, panelRect.width - 40f, panelRect.height - 40f);
        GUILayout.BeginArea(contentRect);
        try
        {
            DrawHand();
        }
        finally
        {
            GUILayout.EndArea();
        }
    }

    private void DrawHand()
    {
        var player = showPlayerOne
            ? GameManager.instance.playerA.Value
            : GameManager.instance.playerB.Value;

        string who = showPlayerOne ? "P1" : "P2";

        if (player == null)
        {
            GUILayout.Label($"{who}: nincs PlayerController.", _emptyStyle, GUILayout.Height(60f));
            return;
        }

        var hand = player.hand;

        GUILayout.Label($"{who} KEZE  •  {hand.Count} lap", _headerStyle, GUILayout.Height(80f));
        GUILayout.Space(14f);

        _scroll = GUILayout.BeginScrollView(_scroll, false, true, GUILayout.ExpandHeight(true));
        try
        {
            if (hand.Count == 0)
            {
                GUILayout.Label("Üres a kéz.", _emptyStyle, GUILayout.Height(50f));
                return;
            }

            for (int i = 0; i < hand.Count; i++)
            {
                DrawCard(hand[i]);
            }
        }
        finally
        {
            GUILayout.EndScrollView();
        }
    }

    private void DrawCard(CardState cs)
    {
        var def = CardManager.instance.GetCard(cs.cardId);
        string name = def != null ? def.description : "?";

        GUILayout.BeginVertical(_cardStyle);
        try
        {
            GUILayout.Label($"#{cs.sequenceId}  •  card:{cs.cardId}  •  {name}  •  cost:{cs.currentCost}",
                _cardStyle, GUILayout.Height(56f));

            // A CardState nyers delta-mezõi — ez az, amire kíváncsi vagy
            GUILayout.Label(
                $"attackBonus: {cs.attackBonus}      healthBonus: {cs.healthBonus}",
                _bonusStyle, GUILayout.Height(56f));
        }
        finally
        {
            GUILayout.EndVertical();
        }

        GUILayout.Space(10f);
    }

    private void InitializeStyles()
    {
        if (_stylesInitialized) return;

        _panelTexture = MakeTexture(PanelBeige);
        _headerTexture = MakeTexture(HeaderBrown);
        _cardTexture = MakeTexture(CardCream);
        _bonusTexture = MakeTexture(BonusGreen);
        _emptyTexture = MakeTexture(EmptyPeach);

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _panelTexture },
            padding = new RectOffset(20, 20, 20, 20)
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { background = _headerTexture, textColor = Color.white },
            fontSize = 40,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(16, 16, 10, 10)
        };

        _cardStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _cardTexture, textColor = DarkBrown },
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(16, 16, 10, 10),
            margin = new RectOffset(4, 4, 2, 2),
            wordWrap = true
        };

        _bonusStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _bonusTexture, textColor = DarkBrown },
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(16, 16, 10, 10),
            margin = new RectOffset(4, 4, 2, 2)
        };

        _emptyStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _emptyTexture, textColor = DarkBrown },
            fontSize = 24,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter
        };

        _stylesInitialized = true;
    }

    private Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void OnDestroy()
    {
        DestroyTexture(_panelTexture);
        DestroyTexture(_headerTexture);
        DestroyTexture(_cardTexture);
        DestroyTexture(_bonusTexture);
        DestroyTexture(_emptyTexture);
    }

    private void DestroyTexture(Texture2D tex)
    {
        if (tex == null) return;
        if (Application.isPlaying) Destroy(tex);
        else DestroyImmediate(tex);
    }
}