using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

[ExecuteAlways]
public class PortraitBaker : MonoBehaviour
{
    [Header("Bake Setup")]
    public Camera bakeCamera;
    public Canvas bakeCanvas;

    [Tooltip("Ez csak preview. Bake közben automatikusan eltûnik.")]
    public GameObject previewRoot;

    [Header("Output")]
    public int outputWidth = 512;
    public int outputHeight = 512;

    public string outputFolder = "Assets/BakedPortraits";
    public string fileName = "HeroPortrait";

    public bool overwriteExisting = true;

#if UNITY_EDITOR

    public string Bake()
    {
        if (bakeCamera == null)
        {
            Debug.LogError(
                "PortraitBaker: Bake Camera nincs megadva.",
                this
            );

            return null;
        }

        if (bakeCanvas == null)
        {
            Debug.LogError(
                "PortraitBaker: Bake Canvas nincs megadva.",
                this
            );

            return null;
        }

        if (bakeCanvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            Debug.LogError(
                "PortraitBaker: A Canvas Render Mode legyen Screen Space - Camera.",
                this
            );

            return null;
        }

        // Biztosan a megfelelõ kamerát használja.
        bakeCanvas.worldCamera = bakeCamera;

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        RenderTexture renderTexture = null;
        Texture2D resultTexture = null;

        RenderTexture previousTarget = bakeCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        CameraClearFlags previousClearFlags = bakeCamera.clearFlags;
        Color previousBackgroundColor = bakeCamera.backgroundColor;

        bool previewWasActive = false;

        try
        {
            // -----------------------------
            // Preview kikapcsolása
            // -----------------------------

            if (previewRoot != null)
            {
                previewWasActive = previewRoot.activeSelf;
                previewRoot.SetActive(false);
            }

            // Megvárjuk, hogy a Canvas frissüljön.
            Canvas.ForceUpdateCanvases();

            // -----------------------------
            // Render Texture létrehozása
            // -----------------------------

            renderTexture = new RenderTexture(
                outputWidth,
                outputHeight,
                24,
                RenderTextureFormat.ARGB32
            );

            renderTexture.antiAliasing = 1;

            resultTexture = new Texture2D(
                outputWidth,
                outputHeight,
                TextureFormat.RGBA32,
                false
            );

            // -----------------------------
            // Átlátszó háttér
            // -----------------------------

            bakeCamera.clearFlags = CameraClearFlags.SolidColor;

            bakeCamera.backgroundColor =
                new Color(0f, 0f, 0f, 0f);

            bakeCamera.targetTexture = renderTexture;

            RenderTexture.active = renderTexture;

            GL.Clear(
                true,
                true,
                new Color(0f, 0f, 0f, 0f)
            );

            // -----------------------------
            // Render
            // -----------------------------

            bakeCamera.Render();

            // -----------------------------
            // RenderTexture -> Texture2D
            // -----------------------------

            resultTexture.ReadPixels(
                new Rect(
                    0,
                    0,
                    outputWidth,
                    outputHeight
                ),
                0,
                0
            );

            resultTexture.Apply();

            // -----------------------------
            // PNG mentése
            // -----------------------------

            string safeName =
                SanitizeFileName(fileName);

            string path = Path.Combine(
                outputFolder,
                safeName + ".png"
            );

            path = path.Replace("\\", "/");

            if (!overwriteExisting)
            {
                path =
                    AssetDatabase.GenerateUniqueAssetPath(path);
            }

            byte[] png = resultTexture.EncodeToPNG();

            File.WriteAllBytes(path, png);

            // -----------------------------
            // Unity import
            // -----------------------------

            AssetDatabase.ImportAsset(path);

            ConfigureImporter(path);

            AssetDatabase.Refresh();

            Debug.Log(
                $"Portrait baked: {path}",
                this
            );

            return path;
        }
        finally
        {
            // -----------------------------
            // Kamera visszaállítása
            // -----------------------------

            bakeCamera.targetTexture =
                previousTarget;

            bakeCamera.clearFlags =
                previousClearFlags;

            bakeCamera.backgroundColor =
                previousBackgroundColor;

            RenderTexture.active =
                previousActive;

            // -----------------------------
            // Preview visszakapcsolása
            // -----------------------------

            if (previewRoot != null)
            {
                previewRoot.SetActive(
                    previewWasActive
                );
            }

            Canvas.ForceUpdateCanvases();

            // -----------------------------
            // Cleanup
            // -----------------------------

            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }

            if (resultTexture != null)
            {
                DestroyImmediate(resultTexture);
            }
        }
    }


    private static string SanitizeFileName(
        string name
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Portrait";
        }

        foreach (
            char c in Path.GetInvalidFileNameChars()
        )
        {
            name = name.Replace(c, '_');
        }

        return name;
    }


    private static void ConfigureImporter(
        string path
    )
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(path)
            as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType =
            TextureImporterType.Sprite;

        importer.spriteImportMode =
            SpriteImportMode.Single;

        importer.alphaIsTransparency = true;

        importer.mipmapEnabled = false;

        importer.textureCompression =
            TextureImporterCompression.Uncompressed;

        importer.filterMode =
            FilterMode.Bilinear;

        importer.SaveAndReimport();
    }

#endif
}