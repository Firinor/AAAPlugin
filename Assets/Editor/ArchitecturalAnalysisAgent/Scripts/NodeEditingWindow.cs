using System;
using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class NodeEditingWindow : EditorWindow
    {
        private Node _node;
        private int colorIndex;
        
        public void SetNode(Node node)
        {
            _node = node;
            colorIndex = node.colorIndex;
            GUI.changed = true;
        }
        private void OnGUI()
        {
            if(_node is null) return;
            
            EditorGUILayout.LabelField("Node Properties", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Name:");
            string newTitle = EditorGUILayout.TextField(_node.title);
            if (newTitle != _node.title)
            {
                _node.title = newTitle;
                GUI.changed = true;
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < 7; i++)
            {
                int index = i; //hash
                GUIStyle styleToUse = index == colorIndex ? Style.SelectedNode(index) : Style.SimpleNode(index);
                Vector2 textSize = styleToUse.CalcSize(new GUIContent(_node.title));
               
                float width = Mathf.Max(textSize.x + 40, Style.MinButtonWidth);
                
                if (GUILayout.Button(_node.title, 
                        styleToUse, 
                        new []{GUILayout.Width(width), GUILayout.Height(60)}))
                {
                    colorIndex = index;
                    _node.colorIndex = index;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnLostFocus()
        {
            Close();
        }
    }
}