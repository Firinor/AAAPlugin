using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MultiPanelWindow : EditorWindow
{
    private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
    private Vector2 scrollPosition;

    [MenuItem("FirUtils/Multi Panel Window")]
    public static void ShowWindow()
    {
        var window = CreateInstance<ScriptAnalyzerWindow>();
        window.titleContent = new GUIContent("Multi Panel Window");
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Header section
        DrawSection("Header", new string[] { "Header1", "Header2", "Header3" });
        
        EditorGUILayout.BeginHorizontal();
        
        // Left column
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 3));
        DrawSection("Left", new string[] { "Left1", "Left2", "Left3" });
        EditorGUILayout.EndVertical();
        
        // Center column
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 3));
        DrawSection("Center", new string[] { "Center1", "Center2", "Center3" });
        EditorGUILayout.EndVertical();
        
        // Right column
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 3));
        DrawSection("Right", new string[] { "Right1", "Right2", "Right3" });
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        
        // Footer section
        DrawSection("Footer", new string[] { "Footer1", "Footer2", "Footer3" });
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawSection(string sectionName, string[] capsuleNames)
    {
        EditorGUILayout.LabelField(sectionName, EditorStyles.boldLabel);
        
        foreach (var name in capsuleNames)
        {
            string fullName = $"{sectionName}_{name}";
            if (!expandedStates.ContainsKey(fullName))
            {
                expandedStates[fullName] = false;
            }

            // Draw capsule
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header area (always visible)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
            
            // Toggle button
            if (GUILayout.Button(expandedStates[fullName] ? "▼" : "►", GUILayout.Width(20)))
            {
                expandedStates[fullName] = !expandedStates[fullName];
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Expanded content (visible when expanded)
            if (expandedStates[fullName])
            {
                EditorGUILayout.LabelField($"This is additional info for {name}");
                EditorGUILayout.LabelField($"Section: {sectionName}");
                EditorGUILayout.LabelField($"Created: {System.DateTime.Now}");
                
                // You can add more detailed information here
                EditorGUILayout.Space();
                if (GUILayout.Button("Action Button"))
                {
                    Debug.Log($"Action performed on {fullName}");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
    }
}