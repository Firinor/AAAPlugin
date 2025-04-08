using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
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
        private string selectedAssemblyString;
        private string[] scriptNames;
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
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            assemblyNames = assemblies.Select(a => a.GetName().Name).ToArray();

            if (assemblyNames.Length > 0)
            {
                
                //RefreshScriptsInAssembly(assemblyNames[selectedAssemblyIndex]);
            }
        }
        
        private void OnGUI()
        {
            ResetStyle();
            
            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawCodeSelectionSection();
            EditorGUILayout.Space();
            DrawCodeMapSection();
        
            EditorGUILayout.EndScrollView();
        }

        private void DrawCodeSelectionSection()
        {
            EditorGUILayout.BeginHorizontal();
            
            LeftCodeSelector();
            CodeSellectorToggle();
            RightCodeSellector();
            
            EditorGUILayout.EndHorizontal();
        }

        private void RightCodeSellector()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            Rect assemblyRect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(assemblyRect, new GUIContent($"Assembly: {selectedAssemblyString}"), FocusType.Keyboard))
            {
                GenericMenu assemblyMenu = new GenericMenu();
                for (int i = 0; i < assemblyNames.Length; i++)
                {
                    int index = i;
                    assemblyMenu.AddItem(new GUIContent(selectedAssemblyString), false, () => 
                    {
                        
                    });
                }
                assemblyMenu.DropDown(assemblyRect);
            }
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  buttonStyle))
            {
                //ShowScriptInfo();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            Rect scriptRect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(scriptRect, new GUIContent($"Script: {selectedScriptString}"),
                    FocusType.Keyboard))
            {
                GenericMenu scriptMenu = new GenericMenu();
                for (int i = 0; i < scriptNames.Length; i++)
                {
                    int index = i; // Локальная копия для замыкания
                    scriptMenu.AddItem(new GUIContent(scriptNames[i]), false, () => { });
                }

                scriptMenu.DropDown(scriptRect);
            }
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  buttonStyle))
            {
                //ShowScriptInfo();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
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
            
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  buttonStyle))
            {
                //ShowAssemblyInfo();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            MonoScript newScript = EditorGUILayout.ObjectField(
                    "Select Script", selectedScript, typeof(MonoScript), false) 
                as MonoScript;
            if (newScript)
            {
                selectedScript = newScript;
            }
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image),  buttonStyle))
            {
                //ShowScriptInfo();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawCodeMapSection()
        {
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