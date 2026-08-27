#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PortraitBaker))]
public class PortraitBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Állítsd be a portré Image pozícióját / scale-jét a Scene vagy Game nézetben, majd nyomd meg a Bake PNG gombot.",
            MessageType.Info
        );

        var baker = (PortraitBaker)target;

        if (GUILayout.Button("Bake PNG", GUILayout.Height(40)))
        {
            string path = baker.Bake();

            if (!string.IsNullOrEmpty(path))
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
#endif