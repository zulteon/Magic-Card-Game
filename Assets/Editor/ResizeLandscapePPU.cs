using UnityEditor;
using UnityEngine;

public static class ResizeLandscapeSpritePPU
{
    private const string SPRITE_FOLDER =
        "Assets/Resources/Sprites";

    private const float REFERENCE_HEIGHT = 768f;
    private const float REFERENCE_PPU = 590f;


    [MenuItem("Tools/Cards/Set Landscape Sprite PPU")]
    public static void Run()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { SPRITE_FOLDER }
            );

        int changed = 0;
        int skipped = 0;


        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
                continue;


            // Csak fekvõ képek.
            if (texture.width <= texture.height)
            {
                skipped++;
                continue;
            }


            TextureImporter importer =
                AssetImporter.GetAtPath(path)
                as TextureImporter;

            if (importer == null)
                continue;


            float newPPU =
                texture.height *
                REFERENCE_PPU /
                REFERENCE_HEIGHT;


            // Ha gyakorlatilag már jó, nem reimportáljuk.
            if (Mathf.Abs(
                importer.spritePixelsPerUnit - newPPU
            ) < 0.01f)
            {
                continue;
            }


            importer.spritePixelsPerUnit = newPPU;

            importer.SaveAndReimport();

            changed++;


            Debug.Log(
                $"{texture.name} | " +
                $"{texture.width}x{texture.height} | " +
                $"PPU: {newPPU:F2}"
            );
        }


        Debug.Log(
            $"LANDSCAPE PPU KÉSZ | " +
            $"Módosítva: {changed} | " +
            $"Nem fekvõ: {skipped}"
        );
    }
}