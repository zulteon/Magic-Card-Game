using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Trigger;

public class FaucetPourUI : MonoBehaviour
{
    [Header("Jar")]
    [SerializeField] private ManaJarUI manaJar;

    
    // =========================================================
    // WATER STREAM
    // =========================================================

    [Header("Water Stream")]

    [Tooltip("Image Type = Filled. A hosszú lefolyó víz.")]
    [SerializeField] private Image waterStream;

    [Tooltip("A vízsugár külön alsó lezáró sprite-ja.")]
    [SerializeField] private RectTransform streamBottom;

    [Tooltip("A csap kifolyási pontja.")]
    [SerializeField] private RectTransform streamTopPoint;

    [Tooltip("A pohár szájánál lévő végpont.")]
    [SerializeField] private RectTransform streamBottomPoint;

    [SerializeField] private float streamFallDuration = 0.6f;

    [Tooltip("Elzáráskor ennyi idő alatt fogy el a vízsugár.")]
    [SerializeField] private float streamDrainDuration = 0.7f;


    // =========================================================
    // FAUCETS
    // =========================================================

    [Header("Faucets")]

    [Tooltip("Ez forog amíg rendesen folyik/töltődik a víz.")]
    [SerializeField] private RectTransform pouringFaucet;

    [Tooltip("Ez forog az elzárási/csillapodási fázis alatt.")]
    [SerializeField] private RectTransform closingFaucet;


    [Header("Faucet Rotation")]

    [Range(-2000f, 2000f)]
    [SerializeField] private float pouringFaucetSpeed = 180f;

    [Range(-2000f, 2000f)]
    [SerializeField] private float closingFaucetSpeed = -180f;


    // =========================================================
    // SPLASH SPAWN
    // =========================================================

    [Header("Splash")]

    [SerializeField] private Image splashPrefab;

    [SerializeField] private RectTransform splashParent;

    [Range(1, 30)]
    [SerializeField] private int splashCount = 7;

    [Tooltip("Mekkora vízszintes területen helyezkedjenek el.")]
    [SerializeField] private float splashWidth = 100f;


    // =========================================================
    // SPLASH BASE ROTATION
    // =========================================================

    [Header("Splash Base Rotation")]

    [Tooltip("A sprite nálad ebben a szögben áll egyenesen.")]
    [Range(-360f, 360f)]
    [SerializeField] private float baseAngle = -35f;

    [Tooltip(
        "A legszélső splash ennyivel dől el a középhez képest. " +
        "A dőlés a középtől való távolságból számolódik."
    )]
    [Range(-180f, 180f)]
    [SerializeField] private float edgeTiltAngle = 40f;

    [Tooltip("Ha fordítva dőlnek a széleken, ezt kapcsold át.")]
    [SerializeField] private bool invertEdgeTilt = false;


    // =========================================================
    // SPLASH SIZE
    // =========================================================

    [Header("Splash Size")]

    [Tooltip("Középen lévő splash mérete.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float centerSize = 1f;

    [Tooltip("A legszélső splash mérete.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float edgeSize = 0.7f;


    // =========================================================
    // LIVE REAL ROTATION
    // =========================================================

    [Header("Live Real Rotation")]

    [Tooltip("Folyás közben ennyit billegjen a saját alap szöge körül.")]
    [Range(0f, 180f)]
    [SerializeField] private float liveRealRotationAmount = 15f;

    [Range(0f, 30f)]
    [SerializeField] private float liveRealRotationSpeed = 3f;


    // =========================================================
    // LIVE FAKE ROTATION
    // =========================================================

    [Header("Live Fake Rotation - Width / Height")]

    [Tooltip(
        "Width/Height deformáció folyás közben. " +
        "0.1 enyhe, 0.3 erős, 1 már nagyon látványos."
    )]
    [Range(0f, 2f)]
    [SerializeField] private float liveFakeRotationAmount = 0.18f;

    [Range(0f, 30f)]
    [SerializeField] private float liveFakeRotationSpeed = 2.5f;


    // =========================================================
    // LIVE SCALE
    // =========================================================

    [Header("Live Scale")]

    [Tooltip("Nagyon enyhe méret pulzálás.")]
    [Range(0f, 1f)]
    [SerializeField] private float liveScaleAmount = 0.06f;

    [Range(0f, 30f)]
    [SerializeField] private float liveScaleSpeed = 2f;


    // =========================================================
    // SPLASH TIMING
    // =========================================================

    [Header("Splash Timing")]

    [Tooltip("Spawnkor ennyi idő alatt scale-elődik fel.")]
    [SerializeField] private float splashAppearDuration = 0.2f;

    [Tooltip("A splashek nem egyszerre jelennek meg.")]
    [SerializeField] private float splashSpawnDelay = 0.04f;

    [Tooltip("Elzárás után ennyi idő alatt csillapodnak el.")]
    [SerializeField] private float splashDampDuration = 0.7f;


    // =========================================================
    // JAR FILL
    // =========================================================

    [Header("Jar Fill")]

    [Range(0f, 1f)]
    [SerializeField] private float testFillTarget = 1f;

    [SerializeField] private float fillDuration = 2f;

    [Tooltip("Feltöltés után még ennyi ideig marad teljes erővel a víz.")]
    [SerializeField] private float endHoldDuration = 0.25f;


    // =========================================================
    // INTERNAL SPLASH
    // =========================================================

    private class SplashEntry
    {
        public RectTransform root;
        public RectTransform imageRect;
        public CanvasGroup canvasGroup;

        public float xOffset;

        // Távolság alapján kiszámított alap dőlés.
        public float restAngle;

        // Eredeti width / height edge mérettel együtt.
        public Vector2 baseSize;

        // Mindegyik más ritmusban mozogjon.
        public float realPhase;
        public float fakePhase;
        public float scalePhase;

        public float speedMultiplier;

        public Coroutine motionCoroutine;
    }


    private readonly List<SplashEntry> allSplashes = new();
    private readonly List<SplashEntry> surfaceSplashes = new();

    private bool pouring;
    private bool fillFinished;
    private bool splashesActive;

    private Coroutine pouringFaucetRotation;
    private Coroutine closingFaucetRotation;

    private Quaternion pouringFaucetStartRotation;
    private Quaternion closingFaucetStartRotation;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (pouringFaucet != null)
            pouringFaucetStartRotation =
                pouringFaucet.localRotation;

        if (closingFaucet != null)
            closingFaucetStartRotation =
                closingFaucet.localRotation;
    }


    // =========================================================
    // START
    // =========================================================

    public void StartPour()
    {
        StartPour((int)testFillTarget);
    }


    public IEnumerator StartPour(float targetAmount)
    {
        if (pouring)
            yield  break;
        yield return 
            PourRoutine(
                Mathf.Clamp01(targetAmount)
        );
    }


    // =========================================================
    // COMPLETE ANIMATION
    // =========================================================

    private IEnumerator PourRoutine(float targetAmount)
    {
        pouring = true;
        fillFinished = false;
        splashesActive = true;

        ClearSplashesImmediate();

        PrepareWaterStream();


        // -----------------------------------------------------
        // 1. Első csap forog
        // -----------------------------------------------------

        if (pouringFaucet != null)
        {
            pouringFaucetRotation =
                StartCoroutine(
                    RotateForever(
                        pouringFaucet,
                        pouringFaucetSpeed
                    )
                );
        }


        // -----------------------------------------------------
        // 2. Víz leesik a pohárig
        // -----------------------------------------------------

        yield return StartCoroutine(
            DropWaterStream()
        );


        // -----------------------------------------------------
        // 3. Pohár szájánál splash-ek
        // -----------------------------------------------------

        yield return StartCoroutine(
            SpawnMouthSplashes()
        );


        // -----------------------------------------------------
        // 4. Elindul a pohár feltöltése
        // -----------------------------------------------------

        StartCoroutine(
            FillJar(targetAmount)
        );


        // -----------------------------------------------------
        // 5. Megvárjuk, hogy legyen vízfelszín
        // -----------------------------------------------------

        while (
            !fillFinished &&
            !manaJar.IsSurfaceActive
        )
        {
            yield return null;
        }


        Coroutine surfaceSpawnRoutine = null;

        if (manaJar.IsSurfaceActive)
        {
            surfaceSpawnRoutine =
                StartCoroutine(
                    SpawnSurfaceSplashes()
                );
        }


        // -----------------------------------------------------
        // 6. Felszíni splash-ek követik a vizet
        // -----------------------------------------------------

        while (!fillFinished)
        {
            UpdateSurfaceSplashes();

            yield return null;
        }


        if (surfaceSpawnRoutine != null)
            yield return surfaceSpawnRoutine;


        UpdateSurfaceSplashes();


        // -----------------------------------------------------
        // 7. Kis ideig még teljesen folyik
        // -----------------------------------------------------

        if (endHoldDuration > 0f)
        {
            yield return new WaitForSeconds(
                endHoldDuration
            );
        }


        // -----------------------------------------------------
        // 8. Első csap megáll
        // -----------------------------------------------------

        if (pouringFaucetRotation != null)
        {
            StopCoroutine(
                pouringFaucetRotation
            );

            pouringFaucetRotation = null;
        }


        // -----------------------------------------------------
        // 9. Második csap forog
        // -----------------------------------------------------

        if (closingFaucet != null)
        {
            closingFaucetRotation =
                StartCoroutine(
                    RotateForever(
                        closingFaucet,
                        closingFaucetSpeed
                    )
                );
        }


        // -----------------------------------------------------
        // 10. Élő splash animáció leáll
        // -----------------------------------------------------

        splashesActive = false;

        StopSplashMotion();


        // -----------------------------------------------------
        // 11.
        // Egyszerre:
        //
        // splash csillapodik
        // víz elfogy
        // -----------------------------------------------------

        Coroutine dampRoutine =
            StartCoroutine(
                DampAllSplashes()
            );

        Coroutine drainRoutine =
            StartCoroutine(
                DrainWaterStream()
            );


        yield return dampRoutine;
        yield return drainRoutine;


        // -----------------------------------------------------
        // 12. Második csap megáll
        // -----------------------------------------------------

        if (closingFaucetRotation != null)
        {
            StopCoroutine(
                closingFaucetRotation
            );

            closingFaucetRotation = null;
        }


        waterStream.gameObject.SetActive(false);
        streamBottom.gameObject.SetActive(false);

        allSplashes.Clear();
        surfaceSplashes.Clear();

        pouring = false;
    }


    // =========================================================
    // FAUCET
    // =========================================================

    private IEnumerator RotateForever(
        RectTransform faucet,
        float speed
    )
    {
        while (true)
        {
            faucet.Rotate(
                0f,
            0f,
                speed * Time.deltaTime
            );

            yield return null;
        }
    }


    // =========================================================
    // WATER STREAM SETUP
    // =========================================================

    private void PrepareWaterStream()
    {
        waterStream.gameObject.SetActive(true);
        streamBottom.gameObject.SetActive(true);

        waterStream.type =
            Image.Type.Filled;

        waterStream.fillMethod =
            Image.FillMethod.Vertical;

        // Fentről lefelé jelenik meg.
        waterStream.fillOrigin =
            (int)Image.OriginVertical.Top;

        waterStream.fillAmount = 0f;

        //SetupStreamRect();

        streamBottom.position =
            streamTopPoint.position;

        streamBottom.localScale =
            Vector3.one;
    }


    private void SetupStreamRect()
    {
        RectTransform streamRect =
            waterStream.rectTransform;

        RectTransform parent =
            streamRect.parent as RectTransform;

        if (parent == null)
            return;


        Vector3 localTop =
            parent.InverseTransformPoint(
                streamTopPoint.position
            );

        Vector3 localBottom =
            parent.InverseTransformPoint(
                streamBottomPoint.position
            );


        float height =
            Mathf.Abs(
                localTop.y -
                localBottom.y
            );


        // A teljes Image mérete csap -> pohár.
        // Animáció közben csak a fillAmount változik.

        streamRect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        streamRect.position =
            streamTopPoint.position;


        streamRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );
    }


    // =========================================================
    // STREAM FALL
    // =========================================================

    private IEnumerator DropWaterStream()
    {
        float time = 0f;


        while (
            time <
            streamFallDuration
        )
        {
            time += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    time /
                    streamFallDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            waterStream.fillAmount =
                t;


            // A kis alsó vízvég ténylegesen követi
            // a Filled Image alját.

            streamBottom.position =
                Vector3.Lerp(
                    streamTopPoint.position,
                    streamBottomPoint.position,
                    t
                );


            yield return null;
        }


        waterStream.fillAmount = 1f;

        streamBottom.position =
            streamBottomPoint.position;
    }


    // =========================================================
    // STREAM DRAIN
    // =========================================================

    private IEnumerator DrainWaterStream()
    {
        /*
         * Elzárás után a fennmaradt víz
         * lefelé fogy el.
         */

        waterStream.fillOrigin =
            (int)Image.OriginVertical.Bottom;


        float time = 0f;


        Vector3 bottomStartScale =
            streamBottom.localScale;


        while (
            time <
            streamDrainDuration
        )
        {
            time += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    time /
                    streamDrainDuration
                );

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            waterStream.fillAmount =
                1f - t;


            // Az alsó kis lezárás is elfogy.

            streamBottom.localScale =
                Vector3.Lerp(
                    bottomStartScale,
                    Vector3.zero,
                    t
                );


            yield return null;
        }


        waterStream.fillAmount = 0f;
        streamBottom.localScale = Vector3.zero;
    }


    // =========================================================
    // JAR
    // =========================================================

    private IEnumerator FillJar(
        float targetAmount
    )
    {
        yield return manaJar.FillTo(
            targetAmount,
            fillDuration
        );

        fillFinished = true;
    }


    // =========================================================
    // MOUTH SPLASH
    // =========================================================

    private IEnumerator SpawnMouthSplashes()
    {
        for (
            int i = 0;
            i < splashCount;
            i++
        )
        {
            SplashEntry splash =
                CreateSplash(
                    streamBottomPoint.position
                );


            allSplashes.Add(splash);


            splash.motionCoroutine =
                StartCoroutine(
                    SplashMotionRoutine(
                        splash
                    )
                );


            yield return new WaitForSeconds(
                splashSpawnDelay
            );
        }
    }


    // =========================================================
    // SURFACE SPLASH
    // =========================================================

    private IEnumerator SpawnSurfaceSplashes()
    {
        for (
            int i = 0;
            i < splashCount;
            i++
        )
        {
            SplashEntry splash =
                CreateSplash(
                    manaJar
                        .GetSurfaceWorldPosition()
                );


            allSplashes.Add(splash);
            surfaceSplashes.Add(splash);


            splash.motionCoroutine =
                StartCoroutine(
                    SplashMotionRoutine(
                        splash
                    )
                );


            yield return new WaitForSeconds(
                splashSpawnDelay
            );
        }
    }


    // =========================================================
    // CREATE SPLASH
    // =========================================================

    private SplashEntry CreateSplash(
        Vector3 centerWorld
    )
    {
        // -----------------------------------------------------
        // Wrapper
        // -----------------------------------------------------

        GameObject rootObject =
            new GameObject(
                "WaterSplash",
                typeof(RectTransform),
                typeof(CanvasGroup)
            );


        RectTransform root =
            rootObject
                .GetComponent<RectTransform>();


        CanvasGroup canvasGroup =
            rootObject
                .GetComponent<CanvasGroup>();


        root.SetParent(
            splashParent,
            false
        );


        root.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        root.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        root.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        root.sizeDelta =
            Vector2.zero;


        // -----------------------------------------------------
        // Center
        // -----------------------------------------------------

        Vector3 centerLocal =
            splashParent
                .InverseTransformPoint(
                    centerWorld
                );


        float halfWidth =
            Mathf.Max(
                1f,
                splashWidth * 0.5f
            );


        float xOffset =
            Random.Range(
                -halfWidth,
                halfWidth
            );


        root.localPosition =
            centerLocal +
            new Vector3(
                xOffset,
                0f,
                0f
            );


        // -----------------------------------------------------
        // SIDE
        //
        // -1 = teljesen bal
        //  0 = közép
        // +1 = teljesen jobb
        // -----------------------------------------------------

        float side =
            Mathf.Clamp(
                xOffset / halfWidth,
                -1f,
                1f
            );


        float distanceFromCenter =
            Mathf.Abs(side);


        // =====================================================
        // BASE TILT
        //
        // NINCS RANDOM DŐLÉS.
        //
        // Csak a középtől való távolság
        // és az oldal határozza meg.
        // =====================================================

        float direction =
            invertEdgeTilt
            ? -1f
            : 1f;


        float restAngle =
            baseAngle +
            side *
            edgeTiltAngle *
            direction;


        root.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                restAngle
            );


        // -----------------------------------------------------
        // Image
        // -----------------------------------------------------

        Image image =
            Instantiate(
                splashPrefab,
                root
            );


        image.gameObject.SetActive(true);


        RectTransform imageRect =
            image.rectTransform;


        imageRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        imageRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        imageRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        imageRect.anchoredPosition =
            Vector2.zero;

        imageRect.localRotation =
            Quaternion.identity;


        // =====================================================
        // EDGE SIZE
        // =====================================================

        float sizeMultiplier =
            Mathf.Lerp(
                centerSize,
                edgeSize,
                distanceFromCenter
            );


        Vector2 originalSize =
            imageRect.sizeDelta;


        Vector2 baseSize =
            originalSize *
            sizeMultiplier;


        imageRect.sizeDelta =
            baseSize;


        canvasGroup.alpha =
            1f;


        // =====================================================
        // RANDOM CSAK AZ ANIMÁCIÓ FÁZISÁRA
        //
        // Nem módosítja a spawn alakját/szögét.
        // Csak azért kell, hogy ne egyszerre
        // mozogjon minden splash.
        // =====================================================

        return new SplashEntry
        {
            root = root,
            imageRect = imageRect,
            canvasGroup = canvasGroup,

            xOffset = xOffset,

            restAngle = restAngle,

            baseSize = baseSize,

            realPhase =
                Random.Range(
                    0f,
                    Mathf.PI * 2f
                ),

            fakePhase =
                Random.Range(
                    0f,
                    Mathf.PI * 2f
                ),

            scalePhase =
                Random.Range(
                    0f,
                    Mathf.PI * 2f
                ),

            speedMultiplier =
                Random.Range(
                    0.85f,
                    1.15f
                )
        };
    }


    // =========================================================
    // LIVE SPLASH MOTION
    // =========================================================

    private IEnumerator SplashMotionRoutine(
        SplashEntry splash
    )
    {
        if (
            splash == null ||
            splash.root == null
        )
            yield break;


        // -----------------------------------------------------
        // 1. Scale in
        // -----------------------------------------------------

        splash.root.localScale =
            Vector3.zero;


        float appearTime = 0f;


        while (
            appearTime <
            splashAppearDuration
        )
        {
            if (splash.root == null)
                yield break;
            appearTime +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    appearTime /
                    splashAppearDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            splash.root.localScale =
                Vector3.one * t;


            ApplyLiveSplashMotion(
                splash
            );


            yield return null;
        }


        splash.root.localScale =
            Vector3.one;


        // -----------------------------------------------------
        // 2. Folyamatos élet
        // -----------------------------------------------------

        while (splashesActive)
        {
            if (splash.root == null)
                yield break;


            ApplyLiveSplashMotion(
                splash
            );


            yield return null;
        }
    }


    private void ApplyLiveSplashMotion(
        SplashEntry splash
    )
    {
        float time =
            Time.time *
            splash.speedMultiplier;


        // =====================================================
        // REAL ROTATION
        // =====================================================

        float realWave =
            Mathf.Sin(
                time *
                liveRealRotationSpeed +
                splash.realPhase
            );


        float angle =
            splash.restAngle +
            realWave *
            liveRealRotationAmount;


        splash.root.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );


        // =====================================================
        // FAKE ROTATION
        //
        // Csak ÉLŐ animáció közben.
        //
        // Spawnkor nincs fake deformálás.
        // =====================================================

        float fakeWave =
            Mathf.Sin(
                time *
                liveFakeRotationSpeed +
                splash.fakePhase
            );


        float fakeAmount =
            fakeWave *
            liveFakeRotationAmount;


        /*
         * Exponential stretch.
         *
         * Így nagy Inspector értéket is
         * adhatsz anélkül, hogy negatív
         * width vagy height keletkezne.
         */

        float stretch =
            Mathf.Exp(fakeAmount);


        // =====================================================
        // SCALE PULSE
        // =====================================================

        float scaleWave =
            Mathf.Sin(
                time *
                liveScaleSpeed +
                splash.scalePhase
            );


        float liveScale =
            1f +
            scaleWave *
            liveScaleAmount;


        liveScale =
            Mathf.Max(
                0.05f,
                liveScale
            );


        // =====================================================
        // WIDTH / HEIGHT
        // =====================================================

        splash.imageRect.sizeDelta =
            new Vector2(
                splash.baseSize.x *
                stretch *
                liveScale,

                splash.baseSize.y /
                stretch *
                liveScale
            );
    }


    // =========================================================
    // SURFACE FOLLOW
    // =========================================================

    private void UpdateSurfaceSplashes()
    {
        if (surfaceSplashes.Count == 0)
            return;


        Vector3 surfaceWorld =
            manaJar
                .GetSurfaceWorldPosition();


        Vector3 surfaceLocal =
            splashParent
                .InverseTransformPoint(
                    surfaceWorld
                );


        for (
            int i = 0;
            i < surfaceSplashes.Count;
            i++
        )
        {
            SplashEntry splash =
                surfaceSplashes[i];


            if (
                splash == null ||
                splash.root == null
            )
                continue;


            Vector3 position =
                splash.root.localPosition;


            position.x =
                surfaceLocal.x +
                splash.xOffset;

            position.y =
                surfaceLocal.y;


            splash.root.localPosition =
                position;
        }
    }


    // =========================================================
    // STOP LIVE MOTION
    // =========================================================

    private void StopSplashMotion()
    {
        for (
            int i = 0;
            i < allSplashes.Count;
            i++
        )
        {
            SplashEntry splash =
                allSplashes[i];


            if (
                splash != null &&
                splash.motionCoroutine != null
            )
            {
                StopCoroutine(
                    splash.motionCoroutine
                );

                splash.motionCoroutine =
                    null;
            }
        }
    }


    // =========================================================
    // DAMP SPLASHES
    // =========================================================

    private IEnumerator DampAllSplashes()
    {
        List<Coroutine> running =
            new List<Coroutine>();


        for (
            int i = 0;
            i < allSplashes.Count;
            i++
        )
        {
            SplashEntry splash =
                allSplashes[i];


            if (
                splash == null ||
                splash.root == null
            )
                continue;


            running.Add(
                StartCoroutine(
                    DampSplash(
                        splash
                    )
                )
            );
        }


        foreach (
            Coroutine coroutine
            in running
        )
        {
            yield return coroutine;
        }
    }


    private IEnumerator DampSplash(
        SplashEntry splash
    )
    {
        if (
            splash == null ||
            splash.root == null
        )
            yield break;


        RectTransform root =
            splash.root;


        Vector3 startScale =
            root.localScale;


        Quaternion startRotation =
            root.localRotation;


        Vector2 startSize =
            splash.imageRect.sizeDelta;


        Quaternion restRotation =
            Quaternion.Euler(
                0f,
                0f,
                splash.restAngle
            );


        Vector2 restSize =
            splash.baseSize;


        float time = 0f;


        while (
            time <
            splashDampDuration
        )
        {
            if (root == null)
                yield break;


            time += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    time /
                    splashDampDuration
                );


            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            // Forgás visszanyugszik.
            root.localRotation =
                Quaternion.Lerp(
                    startRotation,
                    restRotation,
                    t
                );


            // Fake width / height visszaáll.
            splash.imageRect.sizeDelta =
                Vector2.Lerp(
                    startSize,
                    restSize,
                    t
                );


            // Közben összezsugorodik.
            root.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    t
                );


            // És elhalványul.
            splash.canvasGroup.alpha =
                1f - t;


            yield return null;
        }


        if (root != null)
            Destroy(
                root.gameObject
            );
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void ClearSplashesImmediate()
    {
        for (
            int i = 0;
            i < allSplashes.Count;
            i++
        )
        {
            if (
                allSplashes[i]?.root
                != null
            )
            {
                Destroy(
                    allSplashes[i]
                        .root.gameObject
                );
            }
        }


        allSplashes.Clear();
        surfaceSplashes.Clear();
    }


    // =========================================================
    // TEST
    // =========================================================

#if UNITY_EDITOR

    [ContextMenu("TEST / Pour")]
    private void TestPour()
    {
        if (!Application.isPlaying)
        {
            Debug.Log(
                "TEST / Pour csak Play Mode-ban működik."
            );

            return;
        }


        StartPour();
    }


    [ContextMenu("TEST / Reset")]
    private void TestReset()
    {
        if (!Application.isPlaying)
            return;


        StopAllCoroutines();


        pouringFaucetRotation = null;
        closingFaucetRotation = null;

        pouring = false;
        fillFinished = false;
        splashesActive = false;


        ClearSplashesImmediate();


        manaJar.SetFill(0f);


        waterStream.fillAmount =
            0f;

        waterStream.gameObject
            .SetActive(false);

        streamBottom.gameObject
            .SetActive(false);


        if (pouringFaucet != null)
        {
            pouringFaucet.localRotation =
                pouringFaucetStartRotation;
        }


        if (closingFaucet != null)
        {
            closingFaucet.localRotation =
                closingFaucetStartRotation;
        }
    }

#endif
}