using System.Collections;
using TMPro;
using UnityEngine;
using static Trigger;

public class PlayerMessageUI : MonoBehaviour
{
    public static PlayerMessageUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float showDuration = 1.2f;
    [SerializeField] private float fadeDuration = 0.2f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void ShowMessage(string englishText)
    {
        string translatedText =
            Translator.Translate(englishText);

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(
            ShowRoutine(translatedText)
        );
    }

    private IEnumerator ShowRoutine(string message)
    {
        messageText.text = message;

        yield return Fade(0f, 1f);

        yield return new WaitForSeconds(showDuration);

        yield return Fade(1f, 0f);

        currentRoutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                from,
                to,
                elapsed / fadeDuration
            );

            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}