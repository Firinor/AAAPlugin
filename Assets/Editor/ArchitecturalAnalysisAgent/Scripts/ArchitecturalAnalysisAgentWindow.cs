using System;
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
        private MonoScript selectedScript;
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
        
        //NodeStyle
        private GUIStyle nodeStyle;
        private GUIStyle selectedNodeStyle;
        
        private GUIStyle buttonStyle;
        
        [MenuItem("FirUtility/Architectural Analysis Agent")]
        public static void ShowWindow()
        {
            GetWindow<ArchitecturalAnalysisAgentWindow>("Architectural Analysis Agent");
        }

        private void OnEnable()
        {
            RefreshAssemblies();
        }

        private void ResetStyle()
        {
            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
        
            selectedNodeStyle = new GUIStyle();
            selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
            selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.stretchHeight = true;
            buttonStyle.padding = new RectOffset();
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.fixedWidth = 20;
            buttonStyle.fixedHeight = 20;
        }
        
        private void RefreshAssemblies()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            assemblyNames = assemblies.Select(a => a.GetName().Name).ToArray();
        }
        
        private void OnGUI()
        {
            ResetStyle();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawCodeSelectionSection();
            DrawCodeMapSection();
            
            EditorGUILayout.EndScrollView();
        }

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
                && GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  buttonStyle))
            {
                ShowAssemblyInfo(selectedAssembly as AssemblyDefinitionAsset);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            MonoScript newScript = EditorGUILayout.ObjectField(
                    "Select Script", selectedScript, typeof(MonoScript), targetBeingEdited: default) 
                as MonoScript;
            if (newScript)
            {
                selectedScript = newScript;
            }
            if (selectedScript is not null 
                && GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  buttonStyle))
            {
                ShowScriptInfo(selectedScript.GetClass());
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
            analysisInfoWindow.SetType(type);
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
                    Repaint();
                });
            }

            string folderSymbol = assemblyGroup ? "d_Folder Icon" : "d_TextAsset Icon";
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image),  buttonStyle))
            {
                assemblyGroup = !assemblyGroup;
            }
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  buttonStyle))
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
                    Repaint();
                });
            }
            folderSymbol = scriptGroup ? "d_Folder Icon" : "d_TextAsset Icon";
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image),  buttonStyle))
            {
                scriptGroup = !scriptGroup;
            }
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  buttonStyle))
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
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCodeMapSection()
        {
            EditorGUILayout.Space();
            
            float headerHeight = 50;
            Rect gridRect = new Rect(0, headerHeight, position.width, position.height - headerHeight);
            
            GUI.BeginClip(gridRect);
            DrawGrid(20, 0.2f, Color.gray);
            DrawGrid(100, 0.4f, Color.gray);
        /*
            DrawNodes();
            DrawConnections();
        
            DrawConnectionLine(Event.current);
        
            ProcessNodeEvents(Event.current);
            ProcessEvents(Event.current);
        */
            GUI.EndClip();
        
            if (GUI.changed) Repaint();
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
    }
}