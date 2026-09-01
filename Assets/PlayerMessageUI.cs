using System.Collections;
using TMPro;
using UnityEngine;
using static Trigger;
/// <summary>
///     ha a kliens önmagának hivja : PlayerMessageUI.instance.ShowMessage(message);
///   ha a szerver önmagának PlayerMessage.Send(this, "Nincs elég mana!");
/// </summary>
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
public static class PlayerMessage
{
    public static void Send( string message, PlayerController pc)
    {
        pc.TargetShowMessage(pc.Owner, message);
    }
}
/*"Not enough Mana."
"No valid target."
"Invalid target."
"It's not your turn."
"Your board is full."
"Your hand is full."
"This minion can't attack."
"This minion has already attacked."
"This minion is sleeping."
"This minion can't be targeted."
"You can't play this card right now."
"You don't have enough Gold."
"No cards left in your deck."
"No valid minions available."
"No valid enemy minions available."
"No valid friendly minions available."
"This effect has no valid target."
"You must choose a target."
"That target is no longer available."
"Action cancelled."*/