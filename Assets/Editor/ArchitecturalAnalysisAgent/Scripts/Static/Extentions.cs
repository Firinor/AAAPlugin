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
        
        public static bool SelectableFoldout(this EditorGUILayout gui, bool foldout, string selectableText, GUIStyle style = null, bool toggleOnLabelClick = false)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            foldout = EditorGUILayout.Foldout(foldout, GUIContent.none, toggleOnLabelClick, Style.Foldout());
            GUILayout.Space(-40);
            Label(gui, selectableText, new GUIStyle(EditorStyles.boldLabel) { richText = true });
            EditorGUILayout.EndHorizontal();

            return foldout;
        }
    }
}