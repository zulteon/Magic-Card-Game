using UnityEditor;
using UnityEngine;

public class SpritePPUWindow : EditorWindow
{
    private const string DEFAULT_FOLDER =
        "Assets/Resources/Sprites";

    private const float REFERENCE_HEIGHT = 592f;
    private const float REFERENCE_PPU = 360f;

    private string folderPath = DEFAULT_FOLDER;

    [MenuItem("Tools/Sprites/Set PPU By Height")]
    public static void ShowWindow()
    {
        SpritePPUWindow window =
            GetWindow<SpritePPUWindow>("Sprite PPU");

        window.folderPath = DEFAULT_FOLDER;
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Sprite PPU beállítás",
            EditorStyles.boldLabel
        );

        folderPath = EditorGUILayout.TextField(
            "Mappa:",
            folderPath
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Futtatás"))
        {
            SetPixelsPerUnit(folderPath);
        }
    }

    private static void SetPixelsPerUnit(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogError(
                $"Nem létezõ Unity mappa: {folder}"
            );

            return;
        }

        string[] guids = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { folder }
        );

        int changed = 0;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
                continue;

            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                continue;

            float newPPU;

            if (texture.width == 448 &&
                texture.height == 592)
            {
                newPPU = REFERENCE_PPU;
            }
            else if (texture.width > texture.height)
            {
                // Fekvõ kép
                newPPU =
                    texture.width
                    / 448f
                    * 360f;
            }
            else
            {
                // Álló / négyzetes kép
                newPPU =
                    texture.height
                    / 592f
                    * 360f;
            }

            if (Mathf.Approximately(
                importer.spritePixelsPerUnit,
                newPPU
            ))
            {
                continue;
            }

            Debug.Log(
                $"{texture.name}: " +
                $"{texture.width}x{texture.height} " +
                $"PPU {importer.spritePixelsPerUnit} -> {newPPU}"
            );

            importer.spritePixelsPerUnit = newPPU;
            importer.SaveAndReimport();

            changed++;
        }

        Debug.Log(
            $"Kész. {changed} sprite módosítva. Mappa: {folder}"
        );
    }
}