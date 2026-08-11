#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FolderColor
{
    // ide sorold fel: útvonal → szín
    static readonly (string path, Color color)[] Folders =
    {
        ("Assets/Real_Cards",           new Color(0.8f, 0.5f, 0.1f, 0.25f)),
        ("Assets/Real_Cards/Abilities", new Color(1f, 0.7f, 0.2f, 0.25f)),
    };

    static FolderColor()
    {
        EditorApplication.projectWindowItemOnGUI += OnGUI;
    }

    static void OnGUI(string guid, Rect rect)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);

        foreach (var (folder, color) in Folders)
        {
            if (path != folder) continue;
            EditorGUI.DrawRect(rect, color);
            break;
        }
    }
}
#endif