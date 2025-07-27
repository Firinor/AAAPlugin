using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public static class Extensions
    {
        public static void Label(this EditorGUILayout gui, string text, GUIStyle style = null, bool defaultOptions = true, GUILayoutOption[] options = null)
        {
            if (style is null)
                style = EditorStyles.boldLabel;

            if (defaultOptions)
            {
                float height = style.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth);
                options = new []{GUILayout.Height(height)};
            }
            
            EditorGUILayout.SelectableLabel(text, style, options);
        }
    }
}