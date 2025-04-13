using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace FirUtility
{
    public class ArchitecturalAnalysisAgentWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        
        //Left mode
        private AssemblyDefinitionAsset selectedAssembly;
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
        //private List<Connection> connections = new List<Connection>();
        
        [MenuItem("FirUtility/Architectural Analysis Agent")]
        public static void ShowWindow()
        {
            GetWindow<ArchitecturalAnalysisAgentWindow>("Architectural Analysis Agent");
        }

        private void OnEnable()
        {
            map = new NodeMapSettings(this);
            map.OnEditNode = OnEditNode;
            map.OnRemoveNode = OnRemoveNode;
            
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
            RightCodeSellector();
            
            EditorGUILayout.EndHorizontal();
        }

        private void LeftCodeSelector()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            selectedAssembly = EditorGUILayout.ObjectField(
                    "Select Assembly", selectedAssembly, typeof(AssemblyDefinitionAsset), false) 
                as AssemblyDefinitionAsset;
            
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
                if (selectedMonoScript is not null)
                {
                    ShowScriptInfo(selectedMonoScript.GetClass());
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Empty script during analysis", "ОК");
                }
            }
            if (GUILayout.Button("▼", Style.Button()))
            {
                if (selectedMonoScript is not null)
                {
                    GenerateNodes(selectedMonoScript.GetClass());
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Empty script during analysis", "ОК");
                }
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

        private void ShowScriptInfo(string typeName, string assemblyName = null)
        {
            if (!Analyzer.GetTypeByName(out Type type, typeName, assemblyName)) return;

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
                ShowScriptInfo(selectedScriptString, selectedAssemblyString);
            }
            if (GUILayout.Button("▼", Style.Button()))
            {
                if(Analyzer.GetTypeByName(out Type type, selectedScriptString, selectedAssemblyString))
                    GenerateNodes(type);
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
            map.Offset = map.DefaultOffset;
            ClearAllNodes();
            
            Repaint();
        }

        private void ClearAllNodes()
        {
            foreach (var node in nodes)
            {
                node.Destroy();
            }
            
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
                20 * map.Zoom,
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
                    //to down
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
                        //ClearNodeSelection();
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
            Vector2 oldMousePos = (e.mousePosition - map.Offset) / map.Zoom;
            map.Zoom *= e.delta.y < 0 ? 1.06382978f : 0.94f;
            map.Zoom = Mathf.Clamp(map.Zoom, 0.1f, 4);
            Vector2 newMousePos = (e.mousePosition - map.Offset) / map.Zoom;

            Vector2 delta = oldMousePos - newMousePos;
            delta *= map.Zoom;
            map.Offset -= delta;
            
            GUI.changed = true;
        }

        private void ProcessContextMenu(Vector2 mousePosition)
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Add node"), false, () =>
            {
                nodes.Add(new Node("NewNode", map, (mousePosition - map.DefaultOffset) ));
            });
            genericMenu.AddItem(new GUIContent("To start position"), false, () =>
            {
                ToStartPoint();
            });
            genericMenu.ShowAsContext();
        }

        private void ToStartPoint()
        {
            map.Offset = new Vector2(position.width/2f, position.height/2f);
        }

        private void ClearNodeSelection()
        {
            if (nodes == null) return;
            
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Unselect();
            }
        }
        
        private void GenerateNodes(Type type)
        {
            int nodeStep = 50;
            int nodeCount = 0;
            
            ClearAllNodes();
            ToStartPoint();
            
            Center();
            Up();
            Right();
            Down();
            Left();
            
            void Center()
            {
                nodes.Add(new Node(type, map, Vector2.zero, Style.GetColorByType(type)));
            }
            void Up()
            {
                Type parent = type.BaseType;
                Type[] interfaces = type.GetInterfaces();

                bool isInterfaces = interfaces is not null && interfaces.Length > 0;
                
                Vector2 offset = new(0, -nodeStep);
                Vector2 classOffset = new(isInterfaces ? -nodeStep : 0, 0);
                
                int index = 1;
                while (parent is not null)
                {
                    nodes.Add(new Node(parent.FullName, map, 
                        classOffset + offset * index, NodeMapSettings.NodeColor.Teal));
                    index++;
                    parent = parent.BaseType;
                }
                if (!isInterfaces) return;
                
                Vector2 interfaceOffset = new(nodeStep, -nodeStep);
                for(var i = 0; i < interfaces.Length; i++)
                {
                    nodes.Add(new Node(interfaces[i].FullName, map, 
                        interfaceOffset +  offset * i, NodeMapSettings.NodeColor.Orange));
                }
            }
            void Right()
            {
                HashSet<Type> usingTypes = new();
                foreach (var info in type.GetFields(Analyzer.AllBindingFlags))
                {
                    usingTypes.UnionWith(Analyzer.GetAllGeneric(info.FieldType));
                }
                foreach (var info in type.GetProperties(Analyzer.AllBindingFlags))
                {
                    usingTypes.UnionWith(Analyzer.GetAllGeneric(info.PropertyType));
                }
                foreach (var constructor in type.GetConstructors(Analyzer.AllBindingFlags))
                {
                    foreach (var parameterInfo in constructor.GetParameters())
                    {
                        usingTypes.UnionWith(Analyzer.GetAllGeneric(parameterInfo.ParameterType));
                    }
                }
                foreach (var method in type.GetMethods(Analyzer.AllBindingFlags))
                {
                    usingTypes.UnionWith(Analyzer.GetAllGeneric(method.ReturnType));
                    
                    foreach (var parameterInfo in method.GetParameters())
                    {
                        usingTypes.UnionWith(Analyzer.GetAllGeneric(parameterInfo.ParameterType));
                    }
                }

                Analyzer.CleareCommonTypes(usingTypes);

                nodeCount = usingTypes.Count;
                int i = 0;
                foreach (var type in usingTypes)
                {
                    nodes.Add(new Node(type, map, GetPosition(i), Style.GetColorByType(type)));
                    i++;
                }
            }
            void Down()
            {
                
            }
            void Left()
            {
                
            }
            
            Vector2 GetPosition(int i)
            {
                int columnCap = Math.Min(10, nodeCount);
                if (columnCap == 0) columnCap = 1;
                
                Vector2Int startPoint = new(nodeStep * 5, nodeStep * -(columnCap-1)/2);
                Vector2Int columnOffset = new(nodeStep * 4, 0);
                Vector2Int rowStep = new(0, nodeStep);

                return startPoint + (columnOffset * (int)(i / columnCap)) + (rowStep * (i % columnCap));
            }
        }

        #endregion
    }
}