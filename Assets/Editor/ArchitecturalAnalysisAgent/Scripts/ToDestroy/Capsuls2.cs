using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MultiPanelWindow2 : EditorWindow
{
    private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
    private Vector2 scrollPosition;
    private string draggedItem;
    private string selectedItem;
    private string detailedDescription = "Select an item to see details";

    [MenuItem("FirUtils/Multi Panel Window 2")]
    public static void ShowWindow()
    {
        GetWindow<MultiPanelWindow2>("Multi Panel Window");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Header section
        DrawSection("Header", new string[] { "Header1", "Header2", "Header3" });
        
        EditorGUILayout.BeginHorizontal();
        
        // Left column with border
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(position.width / 3));
        DrawSection("Left", new string[] { "Left1", "Left2", "Left3" });
        EditorGUILayout.EndVertical();
        
        // Center column with border - only one capsule
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(position.width / 3));
        EditorGUILayout.LabelField("Center", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(detailedDescription, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();
        
        // Right column with border
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(position.width / 3));
        DrawSection("Right", new string[] { "Right1", "Right2", "Right3" });
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        
        // Footer section
        DrawSection("Footer", new string[] { "Footer1", "Footer2", "Footer3" });
        
        EditorGUILayout.EndScrollView();

        HandleDragAndDrop();
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
            Rect capsuleRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Handle drag
            if (Event.current.type == EventType.MouseDown && capsuleRect.Contains(Event.current.mousePosition))
            {
                draggedItem = fullName;
                selectedItem = fullName;
                detailedDescription = $"Detailed info for {name} in {sectionName}\n\nThis is a more comprehensive description of the selected item.";
                Event.current.Use();
            }

            // Header area
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
            
            // Toggle button
            if (GUILayout.Button(expandedStates[fullName] ? "▼" : "►", GUILayout.Width(20)))
            {
                expandedStates[fullName] = !expandedStates[fullName];
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Expanded content
            if (expandedStates[fullName])
            {
                EditorGUILayout.LabelField($"Section: {sectionName}");
                EditorGUILayout.LabelField($"Item: {name}");
                EditorGUILayout.Space();
                if (GUILayout.Button("Select"))
                {
                    selectedItem = fullName;
                    detailedDescription = $"Selected: {fullName}\n\nAdditional details about this item appear here.";
                }
            }
            
            EditorGUILayout.EndVertical();

            // Highlight selected item
            if (selectedItem == fullName)
            {
                EditorGUI.DrawRect(new Rect(capsuleRect.x, capsuleRect.y, 3, capsuleRect.height), Color.blue);
            }
        }
        
        EditorGUILayout.Space();
    }

    private void HandleDragAndDrop()
    {
        if (Event.current.type == EventType.DragExited)
        {
            draggedItem = null;
        }

        if (!string.IsNullOrEmpty(draggedItem) && Event.current.type == EventType.Repaint)
        {
            GUI.Label(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 200, 20), 
                     $"Moving: {draggedItem}", EditorStyles.boldLabel);
        }
    }
}