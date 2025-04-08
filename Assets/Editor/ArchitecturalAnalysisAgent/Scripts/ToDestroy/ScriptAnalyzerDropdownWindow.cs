using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ScriptAnalyzerDropdownWindow : EditorWindow
{
    private string[] assemblyNames;
    private int selectedAssemblyIndex = 0;
    private string[] scriptNames;
    private int selectedScriptIndex = 0;
    private Vector2 scrollPosition;
    private Type selectedType;
    private List<string> fieldsInfo = new List<string>();
    private List<string> methodsInfo = new List<string>();
    private bool showAssemblyDropdown = false;
    private bool showScriptDropdown = false;

    [MenuItem("FirUtils/Script Analyzer (Dropdown)")]
    public static void ShowWindow()
    {
        GetWindow<ScriptAnalyzerDropdownWindow>("Script Analyzer (Dropdown)");
    }

    private void OnEnable()
    {
        RefreshAssemblies();
    }

    private void RefreshAssemblies()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        assemblyNames = assemblies
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => a.GetName().Name)
            .ToArray();

        if (assemblyNames.Length > 0)
        {
            RefreshScriptsInAssembly(assemblyNames[selectedAssemblyIndex]);
        }
    }

    private void RefreshScriptsInAssembly(string assemblyName)
    {
        try
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (assembly != null)
            {
                Type[] types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
                    .ToArray();

                scriptNames = types.Select(t => t.FullName).ToArray();
            }
            else
            {
                scriptNames = new string[0];
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading assembly: {e.Message}");
            scriptNames = new string[0];
        }

        selectedScriptIndex = 0;
        selectedType = null;
        fieldsInfo.Clear();
        methodsInfo.Clear();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        // Выбор сборки через Dropdown
        if (assemblyNames != null && assemblyNames.Length > 0)
        {
            Rect assemblyRect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(assemblyRect, new GUIContent($"Assembly: {assemblyNames[selectedAssemblyIndex]}"), FocusType.Keyboard))
            {
                GenericMenu assemblyMenu = new GenericMenu();
                for (int i = 0; i < assemblyNames.Length; i++)
                {
                    int index = i; // Локальная копия для замыкания
                    assemblyMenu.AddItem(new GUIContent(assemblyNames[i]), false, () => 
                    {
                        selectedAssemblyIndex = index;
                        RefreshScriptsInAssembly(assemblyNames[selectedAssemblyIndex]);
                    });
                }
                assemblyMenu.DropDown(assemblyRect);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No assemblies found", MessageType.Warning);
        }

        // Выбор скрипта через Dropdown
        if (scriptNames != null && scriptNames.Length > 0)
        {
            Rect scriptRect = EditorGUILayout.GetControlRect();
            if (EditorGUI.DropdownButton(scriptRect, new GUIContent($"Script: {(selectedScriptIndex < scriptNames.Length ? scriptNames[selectedScriptIndex] : "None")}"), FocusType.Keyboard))
            {
                GenericMenu scriptMenu = new GenericMenu();
                for (int i = 0; i < scriptNames.Length; i++)
                {
                    int index = i; // Локальная копия для замыкания
                    scriptMenu.AddItem(new GUIContent(scriptNames[i]), false, () => 
                    {
                        selectedScriptIndex = index;
                        AnalyzeScript();
                    });
                }
                scriptMenu.DropDown(scriptRect);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No scripts found in selected assembly", MessageType.Info);
        }

        // Кнопка обновления
        if (GUILayout.Button("Refresh"))
        {
            RefreshAssemblies();
        }

        EditorGUILayout.Space();

        // Отображение информации о полях и методах
        if (selectedType != null)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField($"Analysis of: {selectedType.FullName}", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fields:", EditorStyles.boldLabel);
            foreach (string field in fieldsInfo)
            {
                EditorGUILayout.LabelField(field);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Methods:", EditorStyles.boldLabel);
            foreach (string method in methodsInfo)
            {
                EditorGUILayout.LabelField(method);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void AnalyzeScript()
    {
        fieldsInfo.Clear();
        methodsInfo.Clear();

        if (scriptNames == null || scriptNames.Length == 0 || selectedScriptIndex >= scriptNames.Length)
        {
            selectedType = null;
            return;
        }

        string fullTypeName = scriptNames[selectedScriptIndex];
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyNames[selectedAssemblyIndex]);

        if (assembly != null)
        {
            selectedType = assembly.GetType(fullTypeName);

            if (selectedType != null)
            {
                // Получаем все поля
                FieldInfo[] fields = selectedType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (FieldInfo field in fields)
                {
                    string accessModifier = field.IsPublic ? "public" : 
                        field.IsPrivate ? "private" : 
                        field.IsFamily ? "protected" : 
                        field.IsAssembly ? "internal" : "protected internal";
                    string staticModifier = field.IsStatic ? " static" : "";
                    fieldsInfo.Add($"{accessModifier}{staticModifier} {field.FieldType.Name} {field.Name}");
                }

                // Получаем все методы
                MethodInfo[] methods = selectedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => !m.IsSpecialName)
                    .ToArray();

                foreach (MethodInfo method in methods)
                {
                    string accessModifier = method.IsPublic ? "public" : 
                        method.IsPrivate ? "private" : 
                        method.IsFamily ? "protected" : 
                        method.IsAssembly ? "internal" : "protected internal";
                    string staticModifier = method.IsStatic ? " static" : "";

                    string parameters = string.Join(", ", method.GetParameters()
                        .Select(p => $"{p.ParameterType.Name} {p.Name}"));

                    methodsInfo.Add($"{accessModifier}{staticModifier} {method.ReturnType.Name} {method.Name}({parameters})");
                }
            }
        }
    }
}