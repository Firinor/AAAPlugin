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
        private AssemblyFilterMode assemblyFilterMode = AssemblyFilterMode.Assets;
        private bool assemblyGroup = true;
        private string selectedAssemblyString;
        private HashSet<string> scriptNames;
        private bool scriptGroup = true;
        private bool assemblyFilter = true;
        private string selectedScriptString;

        private NodeMapSettings map;
        
        //Nodes
        private List<Node> nodes = new List<Node>();
        private Node newConnection;
        private Dictionary<AssemblyBindData, List<AssemblyBindData>> assemblyReferences;
        
        [MenuItem("FirUtility/Quick Analysis", priority = 1)]
        public static void ShowScriptsAnalysis()
        {
            Analyzer.ShowAssetsScriptsInfo();
        }
        
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
            map.OnAnalysisNodeByType = GenerateNodes;
            map.OnAnalysisNodeByAssembly = GenerateNodes;
            map.OnAddConnection = AddConnection;
            map.OnRemoveConnections = RemoveConnections;
            map.OnCopyNode = CopyClassNameToClipboard;
            
            RefreshAssemblies();
        }

        private void RefreshAssemblies()
        {
            string locationKey = assemblyFilterMode switch
            {
                AssemblyFilterMode.Assets => "ScriptAssemblies",
                AssemblyFilterMode.Unity => "Library",
                AssemblyFilterMode.System => "",
                _ => throw new ArgumentOutOfRangeException()
            };
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> assembliesConstainsKey = new();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    if(assembly.Location.Contains(locationKey))
                        assembliesConstainsKey.Add(assembly);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            List<string> assemblyNamesList = new();
            foreach (var assembly in assembliesConstainsKey)
            {
                assemblyNamesList.Add(assembly.GetName().Name);
            }
            assemblyNames = assemblyNamesList.ToArray();

            if (assemblies.Any(a => a.GetName().Name == "Assembly-CSharp"))
            {
                selectedAssemblyString = "Assembly-CSharp";
            }
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
                    CopyClassNameToClipboard(selectedScriptString.Split('.').Last());
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
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2.03f));
            
            EditorGUILayout.BeginHorizontal();
            selectedAssembly = EditorGUILayout.ObjectField(
                    "Select Assembly", 
                    selectedAssembly, 
                    typeof(AssemblyDefinitionAsset), 
                    false) 
                as AssemblyDefinitionAsset;

            if (selectedAssembly != null)
            {
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),
                        Style.Button()))
                {
                    Analyzer.ShowAssemblyInfo(selectedAssembly);
                }

                if (GUILayout.Button("▼", Style.Button()))
                {
                    GenerateNodes(selectedAssembly);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            selectedMonoScript = EditorGUILayout.ObjectField(
                    label: "Select Script",
                    selectedMonoScript, 
                    typeof(MonoScript),
                    allowSceneObjects: false) 
                as MonoScript;
            if (selectedMonoScript != null)
            {
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),
                        Style.Button()))
                {
                    
                    Analyzer.ShowScriptInfo(selectedMonoScript);
                }

                if (GUILayout.Button("▼", Style.Button()))
                {
                    GenerateNodes(selectedMonoScript);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void RightCodeSelector()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2.03f));
            
            EditorGUILayout.BeginHorizontal();
            
            Rect assemblyRect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(assemblyRect, new GUIContent($"Assembly: {selectedAssemblyString}"), FocusType.Passive))
            {
                ShowAdvancedDropdown(assemblyRect, assemblyNames, assemblyGroup, (path) =>
                {
                    selectedAssemblyString = path;
                    selectedScriptString = "";
                    RepaintWindow();
                    assemblyFilter = true;
                });
            }

            string assemblyFilterModeString =  assemblyFilterMode switch
                {
                    AssemblyFilterMode.Assets => "Filter by Assets",
                    AssemblyFilterMode.Unity => "Filter by Unity",
                    AssemblyFilterMode.System => "No filter",
                    _ => throw new ArgumentOutOfRangeException()
                };
            if (GUILayout.Button( new GUIContent(assemblyFilterModeString), new GUIStyle(Style.Button()){fixedWidth = 120}))
            {
                assemblyFilterMode = (AssemblyFilterMode)(((int)assemblyFilterMode + 1) % 3);
                RefreshAssemblies();
            }
            string folderSymbol = assemblyGroup ? "d_Folder Icon" : "d_TextAsset Icon";
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image), Style.Button()))
            {
                assemblyGroup = !assemblyGroup;
            }
            if (!String.IsNullOrEmpty(selectedAssemblyString))
            {
                if(GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image), Style.Button()))
                {
                    Analyzer.ShowAssemblyInfo(selectedAssemblyString);
                }
                if (GUILayout.Button("▼", Style.Button()))
                {
                    GenerateNodes(selectedAssemblyString);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            Rect scriptRect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(scriptRect, new GUIContent($"Script: {selectedScriptString}"), FocusType.Passive))
            {
                List<Type> types = new();
                if (assemblyFilter)
                {
                    if (String.IsNullOrEmpty(selectedAssemblyString))
                    {
                        EditorUtility.DisplayDialog("Error", "Select assembly first", "ОК");
                        return;
                    }

                    Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == selectedAssemblyString);

                    types = assembly.GetTypes().ToList();
                }
                else
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        types.AddRange(assembly.GetTypes()
                            .Where(t => !t.ToString().Contains('<')));//Types with the '<' symbol is no interest to us
                    }
                }

                scriptNames = new();
                scriptNames.UnionWith(types.Select(t => t.ToString()));
                
                ShowAdvancedDropdown(scriptRect, scriptNames.ToArray(),  scriptGroup,(path) =>
                {
                    selectedScriptString = path; 
                    RepaintWindow();
                });
            }
            string assemblyFilterIconString = assemblyFilter ? "Assembly filter ON" : "Assembly filter OFF";
            if (GUILayout.Button( new GUIContent(assemblyFilterIconString), new GUIStyle(Style.Button()){fixedWidth = 120}))
            {
                assemblyFilter = !assemblyFilter;
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
                    Type type = null;
                    if (assemblyFilter)
                        Analyzer.GetTypeByName(out type, selectedScriptString, selectedAssemblyString);
                    else
                        Analyzer.GetTypeByName(out type, selectedScriptString);
                    
                    GenerateNodes(type);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void ShowAdvancedDropdown(Rect assemblyRect, string[] content, bool isNeedGroup, Action<string> onSelected)
        {
#if UNITY_2020_1_OR_NEWER
            var dropdown = new NestedSearchDropdown(
                state: new AdvancedDropdownState(),
                content: content,
                isNeedGroup: isNeedGroup,
                onItemSelected: onSelected
            );

            dropdown.Show(assemblyRect);
#else
            GenericMenu menu = new GenericMenu();
    
            foreach (var item in content)
            { 
                menu.AddItem(new GUIContent(item), false, () => onSelected(item));
            }
    
            menu.DropDown(assemblyRect);
#endif
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
        private void RemoveConnections(Node node)
        {
            node.connections = new HashSet<Node>();
            foreach (var otherNode in nodes)
            {
                otherNode.connections.Remove(node);
            }
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
                20 * map.Zoom
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
                bool isNodeCanSelected = true;
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    bool guiChanged = nodes[i].ProcessEvents(e, isNodeCanSelected);
                    if (guiChanged)
                        isNodeCanSelected = false;
                }
                if (!isNodeCanSelected)
                {
                    GUI.changed = true;
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
                        if(newConnection != null)
                            newConnection = null;
                        else
                            ProcessContextMenu(e.mousePosition);
                    }

                    break;

                case EventType.MouseDrag:
                    if (e.button == 0) // Left mouse click
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
            genericMenu.AddItem(new GUIContent("Delete technical nodes"), false, () =>
            {
                ClearTechnicalNodes();
            });
            genericMenu.ShowAsContext();
        }

        private void ClearTechnicalNodes()
        {
            if(nodes.Count() <= 1) return;
            
            for (int i = nodes.Count()-1; i >= 1; i--)
            {
                if (nodes[i].name.Contains('<'))
                {
                    nodes[0].connections.Remove(nodes[i]);
                    nodes[i].connections = null;
                    nodes.Remove(nodes[i]);
                }
            }
        }

        private void ToStartPoint()
        {
            map.Offset = new Vector2(position.width/2f, position.height/2f);
        }

        private void GenerateNodes(MonoScript monoScript)
        {
            Type type = monoScript.GetClass();
            if (monoScript.GetClass() is null)
            {
                Debug.LogError("Couldn't detect type inside the file " + monoScript.name + "!");
                return;
            }
            GenerateNodes(type);
        }

        private void GenerateNodes(string assemblyName)
        {
            if (Analyzer.GetAssemblyByName(out Assembly assembly, assemblyName))
                GenerateNodes(assembly);
            else
            {
                Debug.LogError("Couldn't find the assembly by name!");
            }
        }
        private void GenerateNodes(AssemblyNode assemblyNode)
        {
            GenerateNodes(assemblyNode.Assembly);
        }
        private void GenerateNodes(AssemblyDefinitionAsset assemblyDefinitionAsset)
        {
            Assembly assembly = Analyzer.GetAssemblyByDefinition(assemblyDefinitionAsset);
            GenerateNodes(assembly);
        }
        private void GenerateNodes(Assembly assembly)
        {
            if(assembly is null) return;
            
            Node lastCreatedNode;
            AssemblyNode centerNode;

            int xStep = map.xStep;
            int nodeCount = 0;

            ClearAllNodes();
            ToStartPoint();
            
            assemblyReferences = Analyzer.FindAssemblyReferences();

            AssemblyBindData mainAssembly = assemblyReferences.Keys.FirstOrDefault(
                key => key.Assembly == assembly);

            if (mainAssembly is null)
                mainAssembly = new(){Assembly = assembly};

            Center();
            Right();
            Left();
            
            void Center()//Itself
            {
                centerNode = new AssemblyNode(mainAssembly, map, Vector2.zero);
                nodes.Add(centerNode);
                lastCreatedNode = centerNode;

                xStep = Mathf.Max(xStep, centerNode.name.Length);
            }
            void Right()//Use GUIDs
            {
                if(!assemblyReferences.TryGetValue(mainAssembly, out List<AssemblyBindData> assemblies))
                    return;
                
                nodeCount = assemblies.Count;
                xStep = CalculateXStep(assemblies);

                int i = 0;
                foreach (AssemblyBindData usingAssembly in assemblies)
                {
                    Node newNode = new AssemblyNode(usingAssembly, map, GetPosition(i, isRightSide: true, assemblies.Count, xStep));
                    centerNode.ConnectNode(newNode);
                    nodes.Add(newNode);
                    i++;
                }
            }
            void Left()//Who use this assembly
            {
                List<AssemblyBindData> foundAssemblies = new();

                foreach (KeyValuePair<AssemblyBindData, List<AssemblyBindData>> reference in assemblyReferences)
                {
                    if(reference.Key == mainAssembly
                       || reference.Value is null
                       || reference.Value.Count == 0) continue;
                    
                    if(reference.Value.Contains(mainAssembly))
                        foundAssemblies.Add(reference.Key);
                }
                
                nodeCount = foundAssemblies.Count;
                xStep = CalculateXStep(foundAssemblies);

                int i = 0;
                foreach (AssemblyBindData usingAssembly in foundAssemblies)
                {
                    Node newNode = new AssemblyNode(usingAssembly, map, GetPosition(i, isRightSide: false, foundAssemblies.Count, xStep));
                    newNode.ConnectNode(centerNode);
                    nodes.Add(newNode);
                    i++;
                }
            }
        }

        private void GenerateNodes(TypeNode typeNode)
        {
            if(typeNode is null || typeNode.type is null) return;
            
            GenerateNodes(typeNode.type);
        }
        private void GenerateNodes(Type type)
        {
            if(type is null) return;

            Node lastCreatedNode;
            TypeNode centerNode;
            
            int xStep = map.xStep;
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
                centerNode = new TypeNode(type, map, Vector2.zero, color: Style.GetColorByType(type));
                nodes.Add(centerNode);
                lastCreatedNode = centerNode;

                xStep = Mathf.Max(xStep, centerNode.name.Length);
            }
            void Up()//Parents
            {
                Type parent = type.BaseType;
                Type[] interfaces = type.GetInterfaces();

                bool isInterfaces = interfaces != null && interfaces.Length > 0;
                bool isParents = parent != null;
                
                Vector2 offset = new Vector2(0, -map.yStep);
                Vector2 classOffset = new Vector2(isInterfaces ? -xStep : 0, 0);
                
                int index = 1;
                while (parent != null)
                {
                    Node newNode = new TypeNode(parent, map,
                        classOffset + offset * index, color: Style.GetColorByType(parent));
                    newNode.ConnectNode(lastCreatedNode);
                    nodes.Add(newNode);

                    lastCreatedNode = newNode;
                    index++;
                    parent = parent.BaseType;
                }
                if (!isInterfaces) return;
                
                Vector2 interfaceOffset = new Vector2(isParents ? xStep : 0, -map.yStep * 1.5f);
                for(var i = 0; i < interfaces.Length; i++)
                {
                    Node newNode = new TypeNode(interfaces[i], map,
                        interfaceOffset + offset * i, color: Style.GetColorByType(interfaces[i]));
                    newNode.ConnectNode(centerNode);
                    nodes.Add(newNode);
                }
            }
            void Right()//References
            {
                HashSet<Type> usingTypes = new HashSet<Type>();

                HashSet<Attribute> attributes = new HashSet<Attribute>(type.GetCustomAttributes());
                Analyzer.ClearAttributes(attributes);
                usingTypes.UnionWith(Analyzer.GetRequireComponentTypes(attributes));
                
                foreach (var info in type.GetGenericArguments())
                    usingTypes.UnionWith(Analyzer.GetAllGeneric(info));
                
                foreach (var info in type.GetFields(Analyzer.AllBindingFlags))
                    usingTypes.UnionWith(Analyzer.GetAllGeneric(info.FieldType));
               
                foreach (var info in type.GetProperties(Analyzer.AllBindingFlags)) 
                    usingTypes.UnionWith(Analyzer.GetAllGeneric(info.PropertyType));
                
                foreach (var constructor in type.GetConstructors(Analyzer.AllBindingFlags))
                    foreach (var parameterInfo in constructor.GetParameters())
                        usingTypes.UnionWith(Analyzer.GetAllGeneric(parameterInfo.ParameterType));


                foreach (var method in type.GetMethods(Analyzer.AllBindingFlags))
                {
                    usingTypes.UnionWith(Analyzer.GetAllGeneric(method.ReturnType));
                    foreach (var parameterInfo in method.GetParameters())
                        usingTypes.UnionWith(Analyzer.GetAllGeneric(parameterInfo.ParameterType));
                }

                Analyzer.ClearTypes(usingTypes);
                
                nodeCount = usingTypes.Count + attributes.Count();
                xStep = CalculateXStep(usingTypes, attributes);
                
                int i = 0;
                foreach (Attribute attribute in attributes)
                {
                    Node newNode = new Node("[Attribute " + attribute + "]", map, GetPosition(i, isRightSide: true, nodeCount, xStep),
                        NodeMapSettings.NodeColor.Teal);
                    centerNode.ConnectNode(newNode);
                    nodes.Add(newNode);
                    i++;
                }

                List<string> names = new List<string>();//T, T2, T[]... We will make sure that generic types are not repeated
                foreach (Type usingType in usingTypes)
                {
                    string name = usingType.ToString();
                    if(names.Contains(name))
                        continue;
                    names.Add(name);
                    
                    Node newNode = new TypeNode(usingType, map, GetPosition(i, isRightSide: true, nodeCount, xStep), color: Style.GetColorByType(usingType));
                    centerNode.ConnectNode(newNode);
                    nodes.Add(newNode);
                    i++;
                }
            }
            void Down()//Inheritors
            {
                var inheritors = Analyzer.GetAllInheritorOfType(type);
                if(inheritors is null) return;
                
                Vector2 offset = new Vector2(0, map.yStep);
                int i = 1;
                foreach (Type inheritor in inheritors)
                {
                    Node newNode = new TypeNode(inheritor, map,
                        offset * i, color: Style.GetColorByType(inheritor));
                    if(inheritor.BaseType == centerNode.type)
                        centerNode.ConnectNode(newNode);
                    
                    nodes.Add(newNode);
                    i++;
                }
            }
            void Left()//Users
            {
                HashSet<Type> usersType = Analyzer.GetAllUsagersOfType(type);
                
                Analyzer.ClearTypes(usersType);

                nodeCount = usersType.Count;
                xStep = CalculateXStep(usersType);
                
                int i = 0;
                foreach (var userType in usersType)
                {
                    Node newNode = new TypeNode(userType, map, GetPosition(i, isRightSide: false, nodeCount, xStep), color: Style.GetColorByType(userType));
                    newNode.ConnectNode(centerNode);
                    nodes.Add(newNode);
                    i++;
                }
            }
        }
        
        private Vector2 GetPosition(int i, bool isRightSide, int nodeCount, int xStep)
        {
            int columnCap = Math.Min(10, nodeCount);
            if (columnCap == 0) columnCap = 1;
                
            Vector2Int startPoint = new Vector2Int((isRightSide? 1 : -1) * xStep * 4, map.yStep * -(columnCap-1)/2);
            Vector2Int columnOffset = new Vector2Int((isRightSide? 1 : -1) * xStep * 4, 0);
            Vector2Int rowStep = new Vector2Int(0, map.yStep);

            return startPoint + (columnOffset * (int)(i / columnCap)) + (rowStep * (i % columnCap));
        }
        
        private int CalculateXStep(HashSet<Type> usingTypes = null, HashSet<Attribute> attributes = null)
        {
            int maxTypeNameLength = 0;
            int maxAttributeNameLength = 0;
                
            if (usingTypes is not null && usingTypes.Count > 0)
                maxTypeNameLength = usingTypes.Max(_type => _type.ToString().Length);
                
            if(attributes is not null && attributes.Count > 0)
                maxAttributeNameLength = attributes.Max(_attribute => _attribute.ToString().Length);
                
            return Mathf.Max(maxTypeNameLength + map.gap, maxAttributeNameLength + map.gap);
        }
        private int CalculateXStep(List<AssemblyBindData> usingAssemblies)
        {
            int maxNameLength = 0;
            
            if(usingAssemblies is not null && usingAssemblies.Count > 0)
                maxNameLength = usingAssemblies.Max(data => data.Assembly.GetName().Name.Length);
                
            return maxNameLength + map.gap;
        }

        #endregion
    }
}