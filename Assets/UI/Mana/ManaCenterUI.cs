using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManaCenterUI : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private ManaJarUI manaJar;

    [SerializeField]
    private FaucetPourUI faucet;

    [SerializeField]
    private TMP_Text manaTMPro;

    [SerializeField]
    private Image csap;


    public static ManaCenterUI instance;


    // =========================================================
    // MANA
    // =========================================================

    [Header("Mana")]

    [SerializeField]
    private int maxMana = 10;

    [SerializeField]
    private int currentMana = 0;


    // =========================================================
    // TEST
    // =========================================================

    [Header("Testing")]

    [Range(0, 10)]
    [SerializeField]
    private int testMana = 1;


    public int CurrentMana => currentMana;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        instance = this;
    }


    private void Start()
    {
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            SetMana(8);
        }
    }


    // =========================================================
    // SET MANA
    // =========================================================

    public void SetMana(int mana)
    {
        mana = Mathf.Clamp(
            mana,
            0,
            maxMana
        );


        currentMana = mana;


        // -----------------------------------------------------
        // TEXT
        // -----------------------------------------------------

        manaTMPro.text =
            currentMana.ToString();


        // -----------------------------------------------------
        // TARGET FILL
        // -----------------------------------------------------

        float targetAmount =
            CalculateFillAmount(
                currentMana
            );


        // -----------------------------------------------------
        // NÕ A MANA
        // -----------------------------------------------------

        if (
            targetAmount >
            manaJar.CurrentAmount
        )
        {
            StartCoroutine(
                StartPour(targetAmount)
            );
        }

        // -----------------------------------------------------
        // CSÖKKEN A MANA
        // -----------------------------------------------------

        else
        {
            manaJar.SetFill(
                targetAmount
            );
        }
    }


    // =========================================================
    // POUR
    // =========================================================

    private IEnumerator StartPour(
        float targetAmount
    )
    {
        csap.gameObject.SetActive(true);


        float dur = 0.1f;
        float t = 0f;

        Color baseColor =
            csap.color;


        // =====================================================
        // FADE IN
        // =====================================================

        while (t < dur)
        {
            t += Time.deltaTime;

            float alpha =
                Mathf.Clamp01(
                    t / dur
                );


            csap.color =
                new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    alpha
                );


            yield return null;
        }


        csap.color =
            new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                1f
            );


        // =====================================================
        // TELJES VÍZ ANIMÁCIÓ
        // =====================================================

        yield return faucet.StartPour(
            targetAmount
        );


        // =====================================================
        // FADE OUT
        // =====================================================

        t = 0f;


        while (t < dur)
        {
            t += Time.deltaTime;

            float alpha =
                1f -
                Mathf.Clamp01(
                    t / dur
                );


            csap.color =
                new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    alpha
                );


            yield return null;
        }


        csap.color =
            new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                0f
            );


        csap.gameObject.SetActive(false);
    }


    // =========================================================
    // CALCULATE
    // =========================================================

    private float CalculateFillAmount(
        int mana
    )
    {
        // 0 mana = üres
        if (mana <= 0)
            return 0f;


        // Ez lesz pontosan az 1 mana vízszintje.
        float oneManaAmount =
            manaJar.SurfaceBottomAmount;


        // 1 mana -> 0
        // 10 mana -> 1
        float t =
            Mathf.InverseLerp(
                1f,
                maxMana,
                mana
            );


        // 1 mana = surface bottom
        // 10 mana = teljesen tele
        return Mathf.Lerp(
            oneManaAmount,
            1f,
            t
        );
    }


    // =========================================================
    // INSTANT
    // =========================================================

    public void SetManaInstant(
        int mana
    )
    {
        mana = Mathf.Clamp(
            mana,
            0,
            maxMana
        );


        currentMana = mana;


        manaTMPro.text =
            currentMana.ToString();


        manaJar.SetFill(
            CalculateFillAmount(
                currentMana
            )
        );
    }


    // =========================================================
    // TEST
    // =========================================================

#if UNITY_EDITOR

    [ContextMenu("TEST / Set Mana")]
    private void TestSetMana()
    {
        if (!Application.isPlaying)
            return;

        SetMana(
            testMana
        );
    }


    [ContextMenu("TEST / Set Mana Instant")]
    private void TestSetManaInstant()
    {
        if (!Application.isPlaying)
            return;

        SetManaInstant(
            testMana
        );
    }

#endif
}