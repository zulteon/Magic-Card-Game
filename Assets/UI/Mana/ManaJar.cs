using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ManaJarUI : MonoBehaviour
{
    [SerializeField] private Image waterFill;
    [SerializeField] private RectTransform waterSurface;
    [SerializeField] private CanvasGroup surfaceCanvasGroup;

    [Header("Fill range")]
    [SerializeField] private float minFill = 0.13f;
    [SerializeField] private float maxFill = 0.55f;

    [Header("Surface vertical movement")]
    [SerializeField] private float surfaceActiveFrom = 0.18f;
    [SerializeField] private float surfaceFadeStart = 0.15f;

    [SerializeField] private float surfaceBottomY = 0f;
    [SerializeField] private float surfaceTopY = 200f;

    // Ennyivel legyen lejjebb, amikor halványan elkezd megjelenni.
    [SerializeField] private float preAppearYOffset = -20f;

    [Header("Surface width")]
    [SerializeField] private float startWidthMultiplier = 0.75f;

    // Eddigre érje el a normál szélességet.
    [SerializeField] private float widthGrowUntil = 0.25f;

    [Header("Testing")]
    [Range(0f, 1f)]
    [SerializeField] private float testAmount = 1f;

    private float originalWidth;


    private void Awake()
    {
        originalWidth = waterSurface.sizeDelta.x;
    }


    public void SetFill(float amount)
    {
        currentAmount = Mathf.Clamp01(amount);

        float realFill = Mathf.Lerp(
            minFill,
            maxFill,
            currentAmount
        );
        amount = Mathf.Clamp01(amount);

        // -----------------------------------------------------
        // WATER FILL
        // -----------------------------------------------------

        

        waterFill.fillAmount = realFill;


        // -----------------------------------------------------
        // SURFACE VISIBILITY / FADE
        // -----------------------------------------------------

        bool shouldExist =
            realFill >= surfaceFadeStart;

        waterSurface.gameObject.SetActive(
            shouldExist
        );

        if (!shouldExist)
            return;


        float alpha = Mathf.InverseLerp(
            surfaceFadeStart,
            surfaceActiveFrom,
            realFill
        );

        // Lágyabb átmenet
        alpha = Mathf.SmoothStep(
            0f,
            1f,
            alpha
        );

        surfaceCanvasGroup.alpha = alpha;


        // -----------------------------------------------------
        // SURFACE Y POSITION
        // -----------------------------------------------------

        Vector2 pos =
            waterSurface.anchoredPosition;

        if (realFill < surfaceActiveFrom)
        {
            // Még a tényleges bottom alatt van.
            float preT = Mathf.InverseLerp(
                surfaceFadeStart,
                surfaceActiveFrom,
                realFill
            );

            preT = Mathf.SmoothStep(
                0f,
                1f,
                preT
            );

            pos.y = Mathf.Lerp(
                surfaceBottomY + preAppearYOffset,
                surfaceBottomY,
                preT
            );
        }
        else
        {
            float surfaceT =
                Mathf.InverseLerp(
                    surfaceActiveFrom,
                    maxFill,
                    realFill
                );

            surfaceT = Mathf.SmoothStep(
                0f,
                1f,
                surfaceT
            );

            pos.y = Mathf.Lerp(
                surfaceBottomY,
                surfaceTopY,
                surfaceT
            );
        }

        waterSurface.anchoredPosition = pos;


        // -----------------------------------------------------
        // SURFACE WIDTH
        // -----------------------------------------------------
        /*
        float widthT =
            Mathf.InverseLerp(
                surfaceFadeStart,
                widthGrowUntil,
                realFill
            );

        widthT = Mathf.SmoothStep(
            0f,
            1f,
            widthT
        );

        float widthMultiplier =
            Mathf.Lerp(
                startWidthMultiplier,
                1f,
                widthT
            );

        Vector2 size =
            waterSurface.sizeDelta;

        size.x =
            originalWidth *
            widthMultiplier;

        waterSurface.sizeDelta = size;*/
        Vector2 finalSize = waterSurface.sizeDelta;
        finalSize.x = surfaceWidth;
        waterSurface.sizeDelta = finalSize;
    }
    [SerializeField]
    private float surfaceWidth = 243.6f;

    [SerializeField, Range(0f, 1f)]
    private float currentAmount;

    public float CurrentAmount => currentAmount;
    public float SurfaceBottomAmount
    {
        get
        {
            return Mathf.InverseLerp(
                minFill,
                maxFill,
                surfaceActiveFrom
            );
        }
    }
    public bool IsSurfaceActive
    {
        get
        {
            float realFill = Mathf.Lerp(
                minFill,
                maxFill,
                currentAmount
            );

            return realFill >= surfaceActiveFrom;
        }
    }

    public Vector3 GetSurfaceWorldPosition()
    {
        return waterSurface.position;
    }

    public IEnumerator FillTo(float targetAmount, float duration)
    {
        targetAmount = Mathf.Clamp01(targetAmount);

        float startAmount = currentAmount;

        if (duration <= 0f)
        {
            SetFill(targetAmount);
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(
                time / duration
            );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            SetFill(
                Mathf.Lerp(
                    startAmount,
                    targetAmount,
                    t
                )
            );

            yield return null;
        }

        SetFill(targetAmount);
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (
            waterFill != null &&
            waterSurface != null &&
            surfaceCanvasGroup != null
        )
        {
            if (originalWidth <= 0f)
                originalWidth =
                    waterSurface.sizeDelta.x;

            SetFill(testAmount);
        }
    }
#endif
}