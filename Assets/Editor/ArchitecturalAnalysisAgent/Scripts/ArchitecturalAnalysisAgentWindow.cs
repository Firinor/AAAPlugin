using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirUtility
{
    public class ArchitecturalAnalysisAgentWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        
        //Left mode
        private Object selectedAssembly;
        private MonoScript selectedMonoScript;
        private Type selectedType;

        //toggle
        private bool isRightMode;
        
        //Right mode
        private string[] assemblyNames;
        private bool assemblyGroup;
        private string selectedAssemblyString;
        private string[] scriptNames;
        private bool scriptGroup;
        private string selectedScriptString;
        
        //NodeMap
        private float zoom = 1;
        private Vector2 offset;
        
        //Nodes
        private List<Node> nodes = new List<Node>();
        private Node selectedNode;
        
        //private List<Connection> connections = new List<Connection>();
        
        [MenuItem("FirUtility/Architectural Analysis Agent")]
        public static void ShowWindow()
        {
            GetWindow<ArchitecturalAnalysisAgentWindow>("Architectural Analysis Agent");
        }

        private void OnEnable()
        {
            RefreshAssemblies();
        }
        
        private void RefreshAssemblies()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            assemblyNames = assemblies.Select(a => a.GetName().Name).ToArray();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawCodeSelectionSection();
            DrawCodeMapSection();
            
            EditorGUILayout.EndScrollView();
        } 
#region CodeSelectionSection
        private void DrawCodeSelectionSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            LeftCodeSelector();
            CodeSellectorToggle();
            RightCodeSellector();
            
            EditorGUILayout.EndHorizontal();
        }

        private void LeftCodeSelector()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            AssemblyDefinitionAsset newAssembly = EditorGUILayout.ObjectField(
                    "Select Assembly", selectedAssembly, typeof(AssemblyDefinitionAsset), false) 
                as AssemblyDefinitionAsset;
            if (newAssembly)
            {
                selectedAssembly = newAssembly;
            }
            
            if (selectedAssembly is not null 
                && GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  Style.Button()))
            {
                ShowAssemblyInfo(selectedAssembly as AssemblyDefinitionAsset);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            MonoScript newScript = EditorGUILayout.ObjectField(
                    "Select Script", selectedMonoScript, typeof(MonoScript), targetBeingEdited: default) 
                as MonoScript;
            if (newScript)
            {
                selectedMonoScript = newScript;
            }
            if (selectedMonoScript is not null 
                && GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  Style.Button()))
            {
                ShowScriptInfo(selectedMonoScript.GetClass());
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void ShowAssemblyInfo(AssemblyDefinitionAsset assemblyDefinitionAsset)
        {
            ShowAssemblyInfo(assemblyDefinitionAsset?.name);
        }
        private void ShowAssemblyInfo(string assemblyName)
        {
            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(assemblyName ?? "");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
                
            if (assembly is null)
            {
                EditorUtility.DisplayDialog("Error", "Null assembly during analysis", "ОК");
            }
            else
            {
                var analysisInfoWindow = CreateInstance<AssemblyAnalysisInfoWindow>();
                analysisInfoWindow.SetAssembly(assembly);
                analysisInfoWindow.titleContent = new GUIContent("Assembly: " + assembly.GetName().Name + " info");
                analysisInfoWindow.Show();
            }
        }

        private void ShowScriptInfo(string typeName)
        {
            if (String.IsNullOrEmpty(typeName))
            {
                EditorUtility.DisplayDialog("Error", "Empty script during analysis", "ОК");
                return;
            }
            
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == selectedAssemblyString);

            if (assembly is null)
            {
                EditorUtility.DisplayDialog("Error", "Null assembly during analysis", "ОК");
                return;
            }
            
            Type type = assembly.GetTypes()
                .FirstOrDefault(a => a.FullName == typeName);
            
            if (type is null)
            {
                EditorUtility.DisplayDialog("Error", "Null type during analysis", "ОК");
                return;
            }
            
            ShowScriptInfo(type);
        }
        private void ShowScriptInfo(Type type)
        {
            var analysisInfoWindow = CreateInstance<TypeAnalyzerWindow>();
            analysisInfoWindow.SetType(type, isRightMode? null:selectedMonoScript);
            analysisInfoWindow.titleContent = new GUIContent(type.Name + " info");
            analysisInfoWindow.Show();
        }

        private void RightCodeSellector()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            Rect assemblyRect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(assemblyRect, new GUIContent($"Assembly: {selectedAssemblyString}"), FocusType.Passive))
            {
                ShowAdvancedDropdown(assemblyRect, assemblyNames, assemblyGroup, (path) =>
                {
                    selectedAssemblyString = path;
                    selectedScriptString = "";
                    RepaintWindow();
                });
            }

            string folderSymbol = assemblyGroup ? "d_Folder Icon" : "d_TextAsset Icon";
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image), Style.Button()))
            {
                assemblyGroup = !assemblyGroup;
            }
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image), Style.Button()))
            {
                ShowAssemblyInfo(selectedAssemblyString);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            Rect scriptRect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(scriptRect, new GUIContent($"Script: {selectedScriptString}"), FocusType.Passive))
            {
                if (String.IsNullOrEmpty(selectedAssemblyString))
                {
                    EditorUtility.DisplayDialog("Error", "Select assembly first", "ОК");
                    return;
                }
                
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == selectedAssemblyString);

                Type[] types = assembly.GetTypes();
                scriptNames = types.Select(t => t.FullName).ToArray();
                
                ShowAdvancedDropdown(scriptRect, scriptNames,  scriptGroup,(path) =>
                {
                    selectedScriptString = path; 
                    RepaintWindow();
                });
            }
            folderSymbol = scriptGroup ? "d_Folder Icon" : "d_TextAsset Icon";
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image), Style.Button()))
            {
                scriptGroup = !scriptGroup;
            }
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image), Style.Button()))
            {
                ShowScriptInfo(selectedScriptString);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void ShowAdvancedDropdown(Rect assemblyRect, string[] content, bool isNeedGroup, Action<string> onSelected)
        {
            var dropdown = new NestedSearchDropdown(
                state: new AdvancedDropdownState(),
                content: content,
                isNeedGroup: isNeedGroup,
                onItemSelected: onSelected
            );

            dropdown.Show(assemblyRect);
        }

        private void CodeSellectorToggle()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(0.1f));
            GUIStyle arrowStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 30,
                fixedHeight = 30
            };

            string arrowSymbol = isRightMode ? "▶" : "◀";
            if (GUILayout.Button(arrowSymbol, arrowStyle))
            {
                isRightMode = !isRightMode;
                RepaintWindow();
            }
            EditorGUILayout.EndVertical();
        }
#endregion

#region NodeSection

private void OnEditNode(Node node)
{
    var editWindow = GetWindow<NodeEditingWindow>("Edit " + node.title);
    editWindow.SetNode(node);
    editWindow.Show();
}
private void OnRemoveNode(Node node)
{
    nodes.Remove(node);
}

        private void RepaintWindow()
        {
            zoom = 1;
            offset = Vector2.zero;
            CleareAllNodes();
            
            Repaint();
        }

        private void CleareAllNodes()
        {
            foreach (var node in nodes)
            {
                node.Destroy();
            }

            selectedNode = null;
            nodes = new List<Node>();
        }

        private void DrawCodeMapSection()
        {
            EditorGUILayout.Space();
            
            float headerHeight = 55;
            Rect gridRect = new Rect(0, headerHeight, position.width, position.height - headerHeight);
            
            GUI.BeginClip(gridRect);
            DrawGrid();
        
            ProcessNodeEvents(Event.current);
            ProcessEvents(Event.current);
            
            DrawNodes();
            //DrawConnectionLine();
            
            GUI.EndClip();
        
            if (GUI.changed) Repaint();
        }
        private void DrawNodes()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Draw();
            }
        }

        private void DrawGrid()
        {
            DrawGrid(20f / zoom, 0.2f, Color.gray);
            DrawGrid(100f / zoom, 0.4f, Color.gray);
        }

        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
         
            Vector2 offset = default;
            
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height * 0.9f / gridSpacing);
        
            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
            
            Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);
        
            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset,
                    new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
            }
        
            for (int i = 0; i < heightDivs; i++)
            {
                Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * i, 0) + newOffset,
                    new Vector3(position.width, gridSpacing * i, 0f) + newOffset);
            }
        
            Handles.color = Color.white;
            Handles.EndGUI();
        }
        
        private void ProcessNodeEvents(Event e)
        {
            if (nodes is null) return;
            
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                bool guiChanged = nodes[i].ProcessEvents(e);

                if (guiChanged)
                {
                    GUI.changed = true;
                }
            }
        }
        
        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        ClearNodeSelection();
                    }

                    if (e.button == 1)
                    {
                        ProcessContextMenu(e.mousePosition);
                    }

                    break;

                case EventType.MouseDrag:
                    if (e.button == 0) // Левая кнопка мыши - перемещение
                    {
                        //OnDrag(e.delta);
                    }

                    break;

                case EventType.ScrollWheel:
                    //OnScroll(-e.delta.y);
                    e.Use();
                    break;
            }
        }
        
        private void ProcessContextMenu(Vector2 mousePosition)
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Add node"), false, () =>
            {
                nodes.Add(new Node("NewNode", mousePosition, OnEditNode, OnRemoveNode));
            });
            genericMenu.ShowAsContext();
        }
        
        private void ClearNodeSelection()
        {
            if (nodes == null) return;
            
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].isSelected = false;
            }
        }
#endregion
    }
}