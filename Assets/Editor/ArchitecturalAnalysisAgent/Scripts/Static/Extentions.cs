using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public static class Extensions
    {
        public static void Label(this EditorGUILayout gui, string text, GUIStyle style = null)
        {
            if (style is null)
                style = EditorStyles.boldLabel;
            
            EditorGUILayout.SelectableLabel(text, style, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
    }
}