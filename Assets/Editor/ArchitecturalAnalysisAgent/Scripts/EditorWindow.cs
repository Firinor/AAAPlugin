using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class ArchitecturalAnalysisAgentWindow : EditorWindow
{
    private List<Rect> squares = new List<Rect>();
    private Rect? draggedSquare = null;
    private Vector2 dragOffset;
    private float squareSize = 50f;
    
    private MonoScript selectedScript;
    private Vector2 scrollPosition;
    
    private bool showSettings = true;

    [MenuItem("FirUtils/Architectural Analysis Agent")]
    public static void ShowWindow()
    {
        GetWindow<ArchitecturalAnalysisAgentWindow>("Architectural Analysis Agent");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        Event currentEvent = Event.current;
        Vector2 mousePos = currentEvent.mousePosition;
        
#region TempToDestroy
        // Add square
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
        {
            squares.Add(new Rect(mousePos.x - squareSize/2, mousePos.y - squareSize/2, squareSize, squareSize));
            //currentEvent.Use();
        }

        // start move square
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            for (int i = squares.Count - 1; i >= 0; i--)
            {
                if (squares[i].Contains(mousePos))
                {
                    draggedSquare = squares[i];
                    dragOffset = new Vector2(squares[i].x - mousePos.x, squares[i].y - mousePos.y);
                    //currentEvent.Use();
                    break;
                }
            }
        }

        // move square
        if (draggedSquare.HasValue && currentEvent.type == EventType.MouseDrag)
        {
            Rect rect = draggedSquare.Value;
            rect.x = mousePos.x + dragOffset.x;
            rect.y = mousePos.y + dragOffset.y;
            draggedSquare = rect;
            //currentEvent.Use();
        }

        // End move square
        if (draggedSquare.HasValue && currentEvent.type == EventType.MouseUp)
        {
            for (int i = 0; i < squares.Count; i++)
            {
                if (squares[i] == (Rect)draggedSquare)
                {
                    squares[i] = (Rect)draggedSquare;
                    break;
                }
            }
            draggedSquare = null;
            //currentEvent.Use();
        }
        #endregion
        
        // background
        EditorGUI.DrawRect(new Rect(5, 5, position.width-5, position.height-5), new Color(0.2f, 0.2f, 0.2f));

        // Draw squares
        foreach (var square in squares)
        {
            EditorGUI.DrawRect(square, Color.blue);
        }

        // draggedSquare over all
        if (draggedSquare.HasValue)
        {
            EditorGUI.DrawRect(draggedSquare.Value, Color.green);
        }
        
        EditorGUI.indentLevel++;

        // ScriptField
        selectedScript = (MonoScript)EditorGUILayout.ObjectField(
            "Select Script",
            selectedScript,
            typeof(MonoScript),
            false);

        Type scriptType = selectedScript.GetClass();
        if (scriptType == null)
        {
            EditorGUILayout.HelpBox("Could not get type information for the selected script", MessageType.Warning);
            return;
        }
        
        // Отображение информации о классе
        EditorGUILayout.LabelField("Class: " + scriptType.FullName, EditorStyles.boldLabel);
        
        // Разделитель
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Поля
        EditorGUILayout.LabelField("Fields:", EditorStyles.boldLabel);
        FieldInfo[] fields = scriptType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        
        foreach (FieldInfo field in fields.OrderBy(f => f.Name))
        {
            string accessModifier = field.IsPublic ? "public" : 
                                  field.IsPrivate ? "private" : 
                                  field.IsFamily ? "protected" : 
                                  field.IsAssembly ? "internal" : "protected internal";
            
            string staticModifier = field.IsStatic ? "static " : "";
            
            EditorGUILayout.LabelField($"{accessModifier} {staticModifier}{field.FieldType.Name} {field.Name}");
        }
        
        // Разделитель
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Свойства
        EditorGUILayout.LabelField("Properties:", EditorStyles.boldLabel);
        PropertyInfo[] properties = scriptType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        
        foreach (PropertyInfo property in properties.OrderBy(p => p.Name))
        {
            EditorGUILayout.LabelField($"{property.PropertyType.Name} {property.Name} {{ get; set; }}");
        }
        
        // Разделитель
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Методы
        EditorGUILayout.LabelField("Methods:", EditorStyles.boldLabel);
        MethodInfo[] methods = scriptType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName) // Исключаем методы свойств и событий
            .ToArray();
        
        foreach (MethodInfo method in methods)
        {
            string accessModifier = method.IsPublic ? "public" : 
                                  method.IsPrivate ? "private" : 
                                  method.IsFamily ? "protected" : 
                                  method.IsAssembly ? "internal" : "protected internal";
            
            string staticModifier = method.IsStatic ? "static " : "";
            
            string parameters = string.Join(", ", method.GetParameters()
                .Select(p => $"{p.ParameterType.Name} {p.Name}"));
            
            EditorGUILayout.LabelField($"{accessModifier} {staticModifier}{method.ReturnType.Name} {method.Name}({parameters})");
        }
                               
        EditorGUILayout.EndScrollView();
        
        // Refresh window
        if (currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseMove)
        {
            Repaint();
        }
    }
}