using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuntimeTextToAbilityUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;
    public Button parseButton;
    public TextMeshProUGUI resultText;
    public Button[] exampleButtons;

    [Header("Generated Effect")]
    public Effect currentEffect;

    [Header("Example Texts")]
    public string[] exampleTexts = {
        "Deal 5 damage to all enemies",
        "Heal 3 health to your hero",
        "Deal 2 damage to a random enemy minion",
        "Heal 4 health to all friendly minions"
    };

    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        // Parse button setup
        if (parseButton != null)
        {
            parseButton.onClick.AddListener(ParseCurrentText);
        }

        // Example buttons setup
        if (exampleButtons != null && exampleButtons.Length > 0)
        {
            for (int i = 0; i < exampleButtons.Length; i++)
            {
                if (i < exampleTexts.Length && exampleButtons[i] != null)
                {
                    int index = i; // Local copy for closure
                    exampleButtons[i].onClick.AddListener(() => SetExampleText(index));

                    // Set button text if it has a Text component
                    var buttonText = exampleButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null && i < exampleTexts.Length)
                    {
                        buttonText.text = exampleTexts[i];
                    }
                }
            }
        }

        // Default text
        if (inputField != null)
        {
            inputField.text = "Deal 5 damage to all enemies";
        }
    }

    public void ParseCurrentText()
    {
        if (inputField == null) return;

        string text = inputField.text;

        if (string.IsNullOrEmpty(text))
        {
            ShowResult("Empty input!");
            return;
        }

        // Parse the text
        currentEffect = TextToAbilityParser.ParseTextToEffect(text);

        if (currentEffect != null)
        {
            string result = FormatEffectResult(currentEffect);
            ShowResult(result);
            Debug.Log($"Successfully parsed: '{text}' -> {TextToAbilityParser.EffectToText(currentEffect)}");
        }
        else
        {
            ShowResult("Failed to parse text!");
            Debug.LogWarning($"Failed to parse: '{text}'");
        }
    }

    private string FormatEffectResult(Effect effect)
    {
        if (effect == null) return "Null effect";

        return $"<b>Effect Generated:</b>\n" +
               $"Type: <color=yellow>{effect.type}</color>\n" +
               $"Value: <color=cyan>{effect.value}</color>\n" +
               $"Target Cast: <color=green>{effect.targetCast}</color>\n" +
               $"Target: <color=red>{effect.target}</color>\n" +
               $"Target Type: <color=magenta>{effect.targetType}</color>\n\n" +
               $"<b>Parsed as:</b>\n<i>{TextToAbilityParser.EffectToText(effect)}</i>";
    }

    private void ShowResult(string result)
    {
        if (resultText != null)
        {
            resultText.text = result;
        }
    }

    public void SetExampleText(int index)
    {
        if (inputField != null && index >= 0 && index < exampleTexts.Length)
        {
            inputField.text = exampleTexts[index];
        }
    }

    // Runtime testing
    [ContextMenu("Test All Examples")]
    public void TestAllExamples()
    {
        Debug.Log("=== Runtime Testing All Examples ===");

        foreach (string text in exampleTexts)
        {
            Effect effect = TextToAbilityParser.ParseTextToEffect(text);
            if (effect != null)
            {
                Debug.Log($"✓ '{text}' -> {TextToAbilityParser.EffectToText(effect)}");
            }
            else
            {
                Debug.LogWarning($"✗ Failed: '{text}'");
            }
        }
    }

    // Method to save effect as asset (csak Editor-ban működik)
    public void SaveCurrentEffect()
    {
        if (currentEffect == null)
        {
            Debug.LogWarning("No effect to save!");
            return;
        }

#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Effect Asset",
            "RuntimeGeneratedEffect",
            "asset",
            "Choose where to save the effect asset");

        if (!string.IsNullOrEmpty(path))
        {
            UnityEditor.AssetDatabase.CreateAsset(currentEffect, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Effect saved to: {path}");
        }
#else
        Debug.Log("Saving assets only works in Editor mode!");
#endif
    }
}