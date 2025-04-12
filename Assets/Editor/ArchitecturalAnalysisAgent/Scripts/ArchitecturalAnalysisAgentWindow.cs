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

        private NodeMapSettings map;
        
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
            map = new NodeMapSettings(this);
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
            
            map.Zoom = float.Parse(EditorGUILayout.TextField(map.Zoom.ToString()));
            EditorGUILayout.TextField(map.Offset.ToString());
            
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
            map.Zoom = 1;
            map.Offset = Vector2.zero;
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
            foreach (var node in nodes)
            {
                node.Draw();
            }
        }

        private void DrawGrid()
        {
            DrawGrid(20f * map.Zoom, 0.2f, Color.gray);
            DrawGrid(100f * map.Zoom, 0.6f, Color.gray);
        }

        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);
        
            Handles.BeginGUI();
            
            Vector2 newOffset = new Vector2(
                map.Offset.x % gridSpacing, 
                map.Offset.y % gridSpacing);
        
            Handles.color = Color.white;
            Handles.DrawWireArc(
                map.Offset,
                Vector3.forward,     
                Vector3.up,   
                360f,          
                20 / map.Zoom,
                3
            );
            
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
            VerticalGrid();
            HorizontalGrid();
            
            Handles.EndGUI();

            void VerticalGrid()
            {
                for (int i = 0; i <= widthDivs; i++)
                {
                    //to right
                    Handles.DrawLine(new Vector3(newOffset.x + gridSpacing * i, 0, 0),
                        new Vector3(newOffset.x + gridSpacing * i, position.height, 0));
                }
            }
            void HorizontalGrid()
            {
                for (int i = 0; i <= heightDivs; i++)
                {
                    Handles.DrawLine(new Vector3(0, newOffset.y + gridSpacing * i, 0),
                        new Vector3(position.width, newOffset.y + gridSpacing * i, 0));
                }
            }
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
                        OnDrag(e.delta);
                    }

                    break;

                case EventType.ScrollWheel:
                    OnScroll(e);
                    e.Use();
                    break;
            }
        }

        private void OnDrag(Vector2 delta)
        {
            map.Offset += delta;
            GUI.changed = true;
        }

        private void OnScroll(Event e)
        {
            float oldZoom = map.Zoom;

            Vector2 center = new Vector2(position.width / 2f, position.height / 2f);
            Vector2 oldMousePos = (e.mousePosition - map.Offset) / map.Zoom;
            Debug.Log("mouse: " + e.mousePosition + " mouseVector:" + (e.mousePosition - center));
            Debug.Log("old: " + oldMousePos + " Zoom:" + map.Zoom);
            map.Zoom *= e.delta.y < 0 ? 1.06382978f : 0.94f;
            map.Zoom = Mathf.Clamp(map.Zoom, 0.1f, 4);
            
            Vector2 newMousePos = (e.mousePosition - map.Offset) / map.Zoom;
            Debug.Log("new: " + newMousePos + " Zoom:" + map.Zoom);

            Vector2 delta = oldMousePos - newMousePos;
            Debug.Log("delta: " + delta + " Zoom:" + (oldZoom - map.Zoom));
            delta *= map.Zoom;
            map.Offset -= delta;
            
            GUI.changed = true;
        }

        private void ProcessContextMenu(Vector2 mousePosition)
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Add node"), false, () =>
            {
                nodes.Add(new Node("NewNode", map, mousePosition, OnEditNode, OnRemoveNode));
            });
            genericMenu.AddItem(new GUIContent("Position"), false, () =>
            {
                map.Offset = new Vector2(position.width/2f, position.height/2f);
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