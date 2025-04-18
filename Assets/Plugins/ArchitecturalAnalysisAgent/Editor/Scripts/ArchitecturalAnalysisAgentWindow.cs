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
        private Node newConnection;
        
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
            map.OnAnalysisNode = GenerateNodes;
            map.OnAddConnection = AddConnection;
            map.OnCopyNode = CopyClassNameToClipboard;
            
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
            
            HandleKeyboardEvents();
            
            DrawCodeSelectionSection();
            DrawCodeMapSection();
            
            EditorGUILayout.EndScrollView();
        } 
        
        private void HandleKeyboardEvents()
        {
            if (focusedWindow == this && !String.IsNullOrEmpty(selectedScriptString))
            {
                Event currentEvent = Event.current;
            
                //Ctrl+C (Command+C on Mac)
                if (currentEvent.type == EventType.KeyDown 
                    && currentEvent.keyCode == KeyCode.C 
                    && (currentEvent.control || currentEvent.command))
                {
                    CopyClassNameToClipboard(selectedScriptString.Split(".").Last());
                    currentEvent.Use(); // Marking the event as processed
                }
            }
        }
        
        private void CopyClassNameToClipboard(string data)
        {
            GUIUtility.systemCopyBuffer = data;
            ShowNotification(new GUIContent($"Copied: {GUIUtility.systemCopyBuffer}"));
            Focus();
        }
#region CodeSelectionSection
        private void DrawCodeSelectionSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            LeftCodeSelector();
            RightCodeSelector();
            
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
                Analyzer.ShowAssemblyInfo(selectedAssembly);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            selectedMonoScript = EditorGUILayout.ObjectField(
                    "Select Script", selectedMonoScript, typeof(MonoScript), targetBeingEdited: default) 
                as MonoScript;
            if (selectedMonoScript is not null)
            {
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),
                        Style.Button()))
                {
                    Analyzer.ShowScriptInfo(selectedMonoScript.GetClass());
                }

                if (GUILayout.Button("▼", Style.Button()))
                {
                    GenerateNodes(selectedMonoScript.GetClass());
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void RightCodeSelector()
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
            if (!String.IsNullOrEmpty(selectedAssemblyString) &&
                GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image), Style.Button()))
            {
                Analyzer.ShowAssemblyInfo(selectedAssemblyString);
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

            if (!String.IsNullOrEmpty(selectedScriptString))
            {
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),
                        Style.Button()))
                {
                    Analyzer.ShowScriptInfo(selectedScriptString, selectedAssemblyString);
                }

                if (GUILayout.Button("▼", Style.Button()))
                {
                    if (Analyzer.GetTypeByName(out Type type, selectedScriptString, selectedAssemblyString))
                        GenerateNodes(type);
                }
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
            var editWindow = GetWindow<NodeEditingWindow>("Edit " + node.name);
            editWindow.SetNode(node);
            editWindow.Show();
        }
        private void OnRemoveNode(Node node)
        {
            nodes.Remove(node);
        }
        
        private void AddConnection(Node node)
        {
            newConnection = node;
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
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Destroy();
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
            
            DrawNodeConnections();
            DrawNodes();
            DrawNewConnections(Event.current);
            
            GUI.EndClip();
        
            if (GUI.changed) Repaint();
        }
        private void DrawNewConnections(Event e)
        {
            if(newConnection is null)
                return;

            Vector2 mousePosition = (e.mousePosition - map.Offset);
            
            float directionX = mousePosition.x - newConnection.position.x;
            float directionY = mousePosition.y - newConnection.position.y;
            bool isHorizontal = Mathf.Abs(directionX) > Mathf.Abs(directionY);
            if (isHorizontal)
            {
                directionY = 0;
                directionX = directionX > 0 ? 1 : -1;
            }
            else
            {
                directionX = 0;
                directionY = directionY > 0 ? 1 : -1;
            }

            Vector2 direction = new Vector2(directionX, directionY);
                
            const float arrowSize = 10f;
            
            Vector2 arrowTip = map.Offset + mousePosition;

            Vector2 arrowLeft = arrowTip - (Vector2)(Quaternion.Euler(0, 0, -30) * direction * arrowSize);
            Vector2 arrowRight = arrowTip - (Vector2)(Quaternion.Euler(0, 0, 30) * direction * arrowSize);

            Handles.DrawBezier(
                map.Offset + newConnection.position * map.Zoom,
                arrowTip,
                map.Offset + newConnection.position * map.Zoom + direction * 50f,
                arrowTip - direction * 50f,
                Color.white,
                null,
                4f
            );
            Handles.DrawAAConvexPolygon(arrowTip, arrowLeft, arrowRight, arrowTip);
            
            GUI.changed = true;
        }
        private void DrawNodeConnections()
        {
            Handles.color = Color.white;
            const int limit = 256;
            if(nodes.Count > limit)
                return;
            
            foreach (var node in nodes)
            {
                node.DrawConnections();
            }
        }
        private void DrawNodes()
        {
            const int limit = 512;
            int i = 0;
            foreach (var node in nodes)
            {
                node.Draw();
                i++;
                if (i > limit)
                {
                    Debug.LogError($"The is more than {limit} nodes! Some nodes are not rendered!");
                    break;
                }
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

            if (newConnection is null)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    bool guiChanged = nodes[i].ProcessEvents(e);

                    if (guiChanged)
                    {
                        GUI.changed = true;
                    }
                }
            }
            else if(e.type == EventType.MouseDown && e.button == 0)
            {
                TryConnectToNode();
                newConnection = null;
            }

            void TryConnectToNode()
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    if (!nodes[i].rect.Contains(e.mousePosition)) continue;
                    
                    newConnection.ConnectNode(nodes[i]);
                    GUI.changed = true;
                    break;
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
                        if(newConnection is not null)
                            newConnection = null;
                        else
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
            if(type is null) return;
            
            Node lastCreatedNode;
            Node centerNode;
            
            int nodeStep = 50;
            int nodeCount = 0;
            
            ClearAllNodes();
            ToStartPoint();
            
            Center();
            Up();
            Right();
            Down();
            Left();
            
            void Center()//Itself
            {
                centerNode = new Node(type, map, Vector2.zero, Style.GetColorByType(type));
                nodes.Add(centerNode);
                lastCreatedNode = centerNode;
            }
            void Up()//Parents
            {
                Type parent = type.BaseType;
                Type[] interfaces = type.GetInterfaces();

                bool isInterfaces = interfaces is not null && interfaces.Length > 0;
                bool isParents = parent is not null;
                
                Vector2 offset = new(0, -nodeStep);
                Vector2 classOffset = new(isInterfaces ? -nodeStep : 0, 0);
                
                int index = 1;
                while (parent is not null)
                {
                    Node newNode = new Node(parent, map,
                        classOffset + offset * index, Style.GetColorByType(parent));
                    newNode.ConnectNode(lastCreatedNode);
                    nodes.Add(newNode);

                    lastCreatedNode = newNode;
                    index++;
                    parent = parent.BaseType;
                }
                if (!isInterfaces) return;
                
                Vector2 interfaceOffset = new(isParents ? nodeStep : 0, -nodeStep);
                for(var i = 0; i < interfaces.Length; i++)
                {
                    Node newNode = new Node(interfaces[i], map,
                        interfaceOffset + offset * i, Style.GetColorByType(interfaces[i]));
                    newNode.ConnectNode(centerNode);
                    nodes.Add(newNode);
                }
            }
            void Right()//References
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

                Analyzer.ClearCommonTypes(usingTypes);

                nodeCount = usingTypes.Count;
                int i = 0;
                foreach (var type in usingTypes)
                {
                    Node newNode = new Node(type, map, GetPosition(i, isRightSide: true), Style.GetColorByType(type));
                    centerNode.ConnectNode(newNode);
                    nodes.Add(newNode);
                    i++;
                }
            }
            void Down()//Inheritors
            {
                var inheritors = Analyzer.GetAllInheritorOfType(type);
                if(inheritors is null) return;
                
                Vector2 offset = new(0, nodeStep);
                int i = 1;
                foreach (Type inheritor in inheritors)
                {
                    Node newNode = new Node(inheritor, map,
                        offset * i, Style.GetColorByType(inheritor));
                    if(inheritor.BaseType == centerNode.type)
                        centerNode.ConnectNode(newNode);
                    
                    nodes.Add(newNode);
                    i++;
                }
            }
            void Left()//Users
            {
                HashSet<Type> usersType = Analyzer.GetAllUsagersOfType(type);
                
                Analyzer.ClearCommonTypes(usersType);

                nodeCount = usersType.Count;
                int i = 0;
                foreach (var type in usersType)
                {
                    Node newNode = new Node(type, map, GetPosition(i, isRightSide: false), Style.GetColorByType(type));
                    newNode.ConnectNode(centerNode);
                    nodes.Add(newNode);
                    i++;
                }
            }
            
            Vector2 GetPosition(int i, bool isRightSide)
            {
                int columnCap = Math.Min(10, nodeCount);
                if (columnCap == 0) columnCap = 1;
                
                Vector2Int startPoint = new((isRightSide? 1 : -1) * nodeStep * 5, nodeStep * -(columnCap-1)/2);
                Vector2Int columnOffset = new((isRightSide? 1 : -1) * nodeStep * 4, 0);
                Vector2Int rowStep = new(0, nodeStep);

                return startPoint + (columnOffset * (int)(i / columnCap)) + (rowStep * (i % columnCap));
            }
        }

        #endregion
    }
}