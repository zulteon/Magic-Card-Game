using UnityEngine;

public class EffectBagOverlay : MonoBehaviour
{
    [Header("Megjelenítés")]
    public bool show = false;
    public KeyCode toggleKey = KeyCode.F1;

    [Header("Méret")]
    [Range(0.7f, 2f)]
    public float overlayScale = 1f;

    [Range(0.5f, 1f)]
    public float widthPercent = 0.92f;

    [Range(0.5f, 1f)]
    public float heightPercent = 0.88f;

    private Vector2 _scroll;

    private GUIStyle _panelStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _minionHeaderStyle;
    private GUIStyle _effectStyle;
    private GUIStyle _emptyStyle;
    private GUIStyle _footerStyle;

    private Texture2D _panelTexture;
    private Texture2D _headerTexture;
    private Texture2D _minionTexture;
    private Texture2D _effectTexture;
    private Texture2D _emptyTexture;

    private bool _stylesInitialized;

    private static readonly Color DarkBrown =
        new Color32(58, 40, 30, 255);

    private static readonly Color HeaderBrown =
        new Color32(105, 73, 50, 255);

    private static readonly Color PanelBeige =
        new Color32(231, 213, 180, 250);

    private static readonly Color MinionBeige =
        new Color32(204, 177, 132, 255);

    private static readonly Color EffectCream =
        new Color32(249, 239, 215, 255);

    private static readonly Color EmptyPeach =
        new Color32(237, 196, 158, 255);

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            show = !show;
        }
    }

    private void OnGUI()
    {
        if (!show || GameManager.instance == null)
            return;

        InitializeStyles();

        float panelWidth = Screen.width * widthPercent;
        float panelHeight = Screen.height * heightPercent;

        float x = (Screen.width - panelWidth) * 0.5f;
        float y = (Screen.height - panelHeight) * 0.5f;

        Rect panelRect = new Rect(
            x,
            y,
            panelWidth,
            panelHeight
        );

        GUI.Box(panelRect, GUIContent.none, _panelStyle);

        Rect contentRect = new Rect(
            panelRect.x + 28f,
            panelRect.y + 28f,
            panelRect.width - 56f,
            panelRect.height - 56f
        );

        GUILayout.BeginArea(contentRect);

        try
        {
            DrawOverlay();
        }
        finally
        {
            GUILayout.EndArea();
        }
    }

    private void DrawOverlay()
    {
        var minions = GameManager.instance.MinionLogics;

        int minionCount = minions != null ? minions.Count : 0;

        GUILayout.Label(
            $"LEMUR EFFECT MONITOR     •     MINIONOK: {minionCount}",
            _headerStyle,
            GUILayout.Height(90f)
        );

        GUILayout.Space(18f);

        _scroll = GUILayout.BeginScrollView(
            _scroll,
            false,
            true,
            GUILayout.ExpandHeight(true)
        );

        try
        {
            if (minions == null || minions.Count == 0)
            {
                GUILayout.Label(
                    "Jelenleg nincs aktív minion.",
                    _emptyStyle,
                    GUILayout.Height(80f)
                );

                return;
            }

            /*
             * Indexes ciklus biztonságosabb, ha a lista
             * játék közben módosulhat.
             */
            for (int i = 0; i < minions.Count; i++)
            {
                var minion = minions[i];

                if (minion == null)
                    continue;

                DrawMinion(minion, i);
            }
            DrawCardBags();
        }
        finally
        {
            GUILayout.EndScrollView();
        }

        GUILayout.Space(12f);

        GUILayout.Label(
            $"{toggleKey}: panel elrejtése",
            _footerStyle,
            GUILayout.Height(48f)
        );
    }
    private void DrawCardBags()
    {
        var bags = GameManager.instance.cardBags.All;

        GUILayout.Space(24f);
        GUILayout.Label(
            $"LAPOK EffectBag-JE     •     {bags.Count} db",
            _headerStyle, GUILayout.Height(90f));
        GUILayout.Space(12f);

        if (bags.Count == 0)
        {
            GUILayout.Label("Egyetlen lapnak sincs EffectBag-je.",
                _emptyStyle, GUILayout.Height(80f));
            return;
        }

        foreach (var kv in bags)
        {
            ushort seqId = kv.Key;
            var bag = kv.Value;

            // hol van a lap, és melyik kártya?
            string where = "?";
            string cardInfo = "";
            if (GameManager.instance.TryFindCard(seqId, out var owner, out var zone, out int idx))
            {
                where = zone == Zone.Hand ? "KÉZBEN" : zone.ToString().ToUpperInvariant();
                var cs = owner.GetCard(zone, idx);
                var def = CardManager.instance.GetCard(cs.cardId);
                cardInfo = def != null
                    ? $"  •  {def.description}  (card {cs.cardId})"
                    : $"  •  card {cs.cardId}";
            }

            GUILayout.BeginVertical(_minionHeaderStyle);
            try
            {
                GUILayout.Label($"LAP #{seqId}  •  {where}{cardInfo}",
                    _minionHeaderStyle, GUILayout.Height(66f));
                GUILayout.Space(10f);

                if (bag.Count == 0)
                {
                    GUILayout.Label(
                        "Nincs ebben a zónában aktív effektje.",
                        _emptyStyle, GUILayout.Height(64f));
                }
                else
                {
                    for (int i = 0; i < bag.All.Count; i++)
                    {
                        GUILayout.Label($"{i + 1}. effekt", _effectStyle, GUILayout.Height(48f));
                        DrawEffect(bag.All[i]);
                    }
                }
            }
            finally { GUILayout.EndVertical(); }

            GUILayout.Space(18f);
        }
    }
    private void DrawMinion(MinionLogic minion, int index)
    {
        GUILayout.BeginVertical(_minionHeaderStyle);

        try
        {
            string minionTitle = $"MINION: {minion._sequenceId}";

            /*
             * Ha az m.name nálad nem hasznos, visszateheted:
             
             */
            

            GUILayout.Label(
                minionTitle,
                _minionHeaderStyle,
                GUILayout.Height(66f)
            );

            GUILayout.Space(10f);

            var bag = minion.effectBag;

            if (bag == null)
            {
                GUILayout.Label(
                    "Nincs létrehozott EffectBag.",
                    _emptyStyle,
                    GUILayout.Height(64f)
                );

                return;
            }

            if (bag.Count == 0)
            {
                GUILayout.Label(
                    "Nincs aktív effekt.",
                    _emptyStyle,
                    GUILayout.Height(64f)
                );

                return;
            }

            foreach (var effect in bag.All)
            {
                if (effect == null)
                    continue;

                DrawEffect(effect);
            }
        }
        finally
        {
            GUILayout.EndVertical();
        }

        GUILayout.Space(18f);
    }

    private void DrawEffect(LiveEffect effect)
    {
        if (effect == null)
            return;

        string roleText = effect.Role.ToString().ToUpperInvariant();
        string mainText = GetMainEffectText(effect);
        string detailsText = GetEffectDetails(effect);

        GUILayout.BeginVertical(_effectStyle);

        try
        {
            // Felső sor: ROLE | TYPE vagy BLOCKS
            GUILayout.BeginHorizontal();

            try
            {
                GUILayout.Label(
                    roleText,
                    _effectStyle,
                    GUILayout.Width(230f),
                    GUILayout.Height(62f)
                );

                GUILayout.Label(
                    mainText,
                    _effectStyle,
                    GUILayout.ExpandWidth(true),
                    GUILayout.Height(62f)
                );
            }
            finally
            {
                GUILayout.EndHorizontal();
            }

            // Alsó sor: csak az adott szerepnél releváns állapotok
            if (!string.IsNullOrEmpty(detailsText))
            {
                GUILayout.Space(6f);

                GUILayout.Label(
                    detailsText,
                    _effectStyle,
                    GUILayout.Height(58f)
                );
            }
        }
        finally
        {
            GUILayout.EndVertical();
        }

        GUILayout.Space(10f);
    }
    private string GetMainEffectText(LiveEffect effect)
    {
        if (effect.Role == EffectRole.Guard)
        {
            return $"BLOCKS: {effect.toBlock}";
        }

        return $"TYPE: {TryGetEffectType(effect)}";
    }
    private string GetEffectDetails(LiveEffect effect)
    {
        switch (effect.Role)
        {
            case EffectRole.Guard:
                {
                    string details = $"TRIGGERS: {effect.seen}";

                    if (effect.charges >= 0)
                        details += $"     •     MARADT: {effect.charges}";

                    if (effect.howOften > 1)
                        details += $"     •     MINDEN {effect.howOften}. ALKALOMMAL";

                    return details;
                }

            case EffectRole.Trigger:
                {
                    string details = $"TRIGGERS: {effect.seen}";

                    if (effect.howOften > 1)
                        details += $"     •     MINDEN {effect.howOften}. ALKALOMMAL";

                    return details;
                }

            case EffectRole.Delayed:
                {
                    string details = $"TRIGGERS: {effect.seen}";

                    if (effect.charges >= 0)
                        details += $"     •     MARADT: {effect.charges}";

                    if (effect.remainingTurns >= 0)
                        details += $"     •     LEJÁR: {effect.remainingTurns}. KÖR";

                    return details;
                }

            case EffectRole.Aura:
                return string.Empty;

            default:
                return string.Empty;
        }
    }
    private string TryGetEffectType(LiveEffect effect)
    {
        if (effect == null)
            return "UNKNOWN";

        if (effect.Def == null)
            return $"UNKNOWN ({effect.effectId})";

        return effect.Def.type.ToString().ToUpperInvariant();
    }
    private void InitializeStyles()
    {
        if (_stylesInitialized)
            return;

        _panelTexture = MakeTexture(PanelBeige);
        _headerTexture = MakeTexture(HeaderBrown);
        _minionTexture = MakeTexture(MinionBeige);
        _effectTexture = MakeTexture(EffectCream);
        _emptyTexture = MakeTexture(EmptyPeach);

        int headerFont = Mathf.RoundToInt(46f * overlayScale);
        int minionFont = Mathf.RoundToInt(38f * overlayScale);
        int effectFont = Mathf.RoundToInt(32f * overlayScale);
        int emptyFont = Mathf.RoundToInt(30f * overlayScale);
        int footerFont = Mathf.RoundToInt(25f * overlayScale);

        _panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal =
            {
                background = _panelTexture
            },

            padding = new RectOffset(20, 20, 20, 20)
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            normal =
            {
                background = _headerTexture,
                textColor = Color.white
            },

            fontSize = headerFont,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(20, 20, 12, 12),
            wordWrap = false
        };

        _minionHeaderStyle = new GUIStyle(GUI.skin.box)
        {
            normal =
            {
                background = _minionTexture,
                textColor = DarkBrown
            },

            fontSize = minionFont,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(20, 20, 16, 16),
            margin = new RectOffset(4, 18, 4, 10),
            wordWrap = true
        };

        _effectStyle = new GUIStyle(GUI.skin.box)
        {
            normal =
            {
                background = _effectTexture,
                textColor = DarkBrown
            },

            fontSize = effectFont,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(18, 18, 12, 12),
            margin = new RectOffset(6, 6, 3, 3),
            wordWrap = true
        };

        _emptyStyle = new GUIStyle(GUI.skin.box)
        {
            normal =
            {
                background = _emptyTexture,
                textColor = DarkBrown
            },

            fontSize = emptyFont,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(18, 18, 12, 12),
            margin = new RectOffset(6, 6, 4, 4),
            wordWrap = true
        };

        _footerStyle = new GUIStyle(GUI.skin.label)
        {
            normal =
            {
                textColor = DarkBrown
            },

            fontSize = footerFont,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter
        };

        _stylesInitialized = true;
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1)
        {
            name = "EffectBagOverlayTexture",
            hideFlags = HideFlags.HideAndDontSave
        };

        texture.SetPixel(0, 0, color);
        texture.Apply();

        return texture;
    }

    private void OnValidate()
    {
        /*
         * Inspectorban történő méretmódosítás után
         * újrageneráljuk a fontméreteket.
         */
        _stylesInitialized = false;
    }

    private void OnDestroy()
    {
        DestroyTexture(_panelTexture);
        DestroyTexture(_headerTexture);
        DestroyTexture(_minionTexture);
        DestroyTexture(_effectTexture);
        DestroyTexture(_emptyTexture);
    }

    private void DestroyTexture(Texture2D texture)
    {
        if (texture == null)
            return;

        if (Application.isPlaying)
            Destroy(texture);
        else
            DestroyImmediate(texture);
    }
}