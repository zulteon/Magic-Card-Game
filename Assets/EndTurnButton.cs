using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndTurnButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image hourglass;

    [Header("Rotation")]
    [SerializeField] private float rotateDuration = 0.35f;

    [Header("Warning Blink")]
    [SerializeField] private float blinkSpeed = 4f;

    private Coroutine rotateCoroutine;
    private Coroutine blinkCoroutine;

    private Color normalColor;


    private void Awake()
    {
        normalColor = hourglass.color;
    }


    private void Start()
    {
        button.onClick.AddListener(OnClick);

        if (EffectClient.instance != null)
        {
            EffectClient.instance.OnTurnEnd += OnTurnEnd;
            EffectClient.instance.OnTurnStart += OnTurnStart;
        }
    }


    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClick);

        if (EffectClient.instance != null)
        {
            EffectClient.instance.OnTurnEnd -= OnTurnEnd;
            EffectClient.instance.OnTurnStart -= OnTurnStart;
        }
    }


    // =========================================================
    // BUTTON
    // =========================================================

    private void OnClick()
    {
        // A szerver dönti el a kör végét.
        GameManager.instance.GetLocalPlayerController().RequestEndTurnServerRpc();

        // Nem itt forgatjuk meg a homokórát.
        // Megvárjuk a szerverrõl érkezõ TurnEnd eventet.

        button.interactable = false;
    }


    // =========================================================
    // TURN EVENTS
    // =========================================================

    private void OnTurnEnd()
    {
        if (rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        rotateCoroutine = StartCoroutine(
            RotateHourglass180()
        );

        StopRedBlink();

        button.interactable = false;
    }


    private void OnTurnStart()
    {
        // Késõbb itt lehet eldönteni,
        // hogy ténylegesen a mi körünk kezdõdött-e.

        button.interactable = true;

        StopRedBlink();
    }


    // =========================================================
    // ROTATION
    // =========================================================

    private IEnumerator RotateHourglass180()
    {
        RectTransform rect =
            hourglass.rectTransform;

        Quaternion startRotation =
            rect.localRotation;

        Quaternion targetRotation =
            startRotation *
            Quaternion.Euler(
                0f,
                0f,
                180f
            );

        float time = 0f;

        while (time < rotateDuration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    time / rotateDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            rect.localRotation =
                Quaternion.Lerp(
                    startRotation,
                    targetRotation,
                    t
                );

            yield return null;
        }

        rect.localRotation =
            targetRotation;

        rotateCoroutine = null;
    }


    // =========================================================
    // RED WARNING BLINK
    // =========================================================

    public void StartRedBlink()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine =
            StartCoroutine(
                RedBlink()
            );
    }


    public void StopRedBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(
                blinkCoroutine
            );

            blinkCoroutine = null;
        }

        hourglass.color =
            normalColor;
    }


    private IEnumerator RedBlink()
    {
        while (true)
        {
            float t =
                (
                    Mathf.Sin(
                        Time.time *
                        blinkSpeed
                    )
                    + 1f
                )
                * 0.5f;

            hourglass.color =
                Color.Lerp(
                    normalColor,
                    Color.red,
                    t
                );

            yield return null;
        }
    }
}