using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MissingSpriteChecker
{
    private const string CARD_FOLDER =
        "Assets/Real_Cards/cards";

    private const string SPRITE_FOLDER =
        "Assets/Resources/Sprites";


    [MenuItem("Tools/Cards/Check Missing Sprites")]
    public static void CheckMissingSprites()
    {
        // -----------------------------------
        // Meglévõ sprite-ok összegyûjtése
        // -----------------------------------

        HashSet<string> existingSprites =
            new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase
            );

        string[] spriteGuids =
            AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { SPRITE_FOLDER }
            );

        foreach (string guid in spriteGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            string fileName =
                Path.GetFileNameWithoutExtension(path);

            existingSprites.Add(fileName);
        }


        // -----------------------------------
        // Cardok végignézése
        // -----------------------------------

        string[] cardGuids =
            AssetDatabase.FindAssets(
                "t:Card",
                new[] { CARD_FOLDER }
            );

        List<string> missing =
            new List<string>();


        foreach (string guid in cardGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Card card =
                AssetDatabase.LoadAssetAtPath<Card>(path);

            if (card == null)
                continue;


            string spriteName = null;


            if (card.m != null)
            {
                spriteName = card.m.sprite;
            }
            else if (card.spell != null)
            {
                spriteName = card.spell.sprite;
            }


            if (string.IsNullOrWhiteSpace(spriteName))
            {
                Debug.LogWarning(
                    $"Card ID {card.cardId} nem tartalmaz sprite nevet."
                );

                continue;
            }


            spriteName = spriteName.Trim();


            if (!existingSprites.Contains(spriteName))
            {
                if (!missing.Contains(spriteName))
                {
                    missing.Add(spriteName);
                }
            }
        }


        // -----------------------------------
        // Pythonba másolható eredmény
        // -----------------------------------

        missing.Sort();

        StringBuilder sb =
            new StringBuilder();

        sb.AppendLine("missing_sprites = [");

        foreach (string sprite in missing)
        {
            // Python string miatt az idézõjel escape.
            string safe =
                sprite
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");

            sb.AppendLine(
                $"    \"{safe}\","
            );
        }

        sb.AppendLine("]");


        Debug.Log(sb.ToString());

        Debug.Log(
            $"Ellenõrzés kész. " +
            $"Cardok: {cardGuids.Length}, " +
            $"Sprite-ok: {existingSprites.Count}, " +
            $"Hiányzó: {missing.Count}"
        );
    }
}