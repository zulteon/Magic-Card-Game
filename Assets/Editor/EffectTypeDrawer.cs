#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomPropertyDrawer(typeof(Effect.Type))]
public class EffectTypeDrawer : PropertyDrawer
{
    private string _search = "";
    private static readonly string[] _allNames = 
        System.Enum.GetNames(typeof(Effect.Type));

    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        EditorGUI.BeginProperty(pos, label, prop);

        // keresõmezõ
        Rect searchRect = new Rect(pos.x, pos.y, pos.width, 18);
        _search = EditorGUI.TextField(searchRect, "Search", _search);

        // szûrés
        string[] filtered = string.IsNullOrEmpty(_search)
            ? _allNames
            : _allNames.Where(n => n.IndexOf(_search, 
                System.StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

        if (filtered.Length == 0)
        {
            Rect emptyRect = new Rect(pos.x, pos.y + 22, pos.width, 18);
            EditorGUI.LabelField(emptyRect, "Nincs találat");
            EditorGUI.EndProperty();
            return;
        }

        // jelenlegi érték megkeresése a szûrt listában
        string currentName = _allNames[prop.enumValueIndex];
        int currentIndex = System.Array.IndexOf(filtered, currentName);
        if (currentIndex < 0) currentIndex = 0;

        // dropdown
        Rect dropRect = new Rect(pos.x, pos.y + 22, pos.width, 18);
        int selected = EditorGUI.Popup(dropRect, label.text, currentIndex, filtered);

        // visszaírás
        if (selected >= 0 && selected < filtered.Length)
        {
            int realIndex = System.Array.IndexOf(_allNames, filtered[selected]);
            if (realIndex >= 0)
                prop.enumValueIndex = realIndex;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) 
        => 44f;
}
#endif