using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;

public class ScriptAnalyzerWindowAllMetods : EditorWindow
{
    private MonoScript selectedScript;
    private Vector2 scrollPosition;
    
    private Dictionary<string, List<string>> fieldsByType = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> methodsByType = new Dictionary<string, List<string>>();
    
    [MenuItem("FirUtils/Script Analyzer All Metods")]
    public static void ShowWindow()
    {
        GetWindow<ScriptAnalyzerWindowAllMetods>("Script Analyzer");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Script Analyzer", EditorStyles.boldLabel);
        
        // Поле для выбора скрипта
        MonoScript newScript = (MonoScript)EditorGUILayout.ObjectField("Select Script", selectedScript, typeof(MonoScript), false);
        
        if (newScript != selectedScript)
        {
            selectedScript = newScript;
            AnalyzeScript();
        }
        
        if (selectedScript == null)
        {
            EditorGUILayout.HelpBox("Please select a script to analyze", MessageType.Info);
            return;
        }
        
        // Отображение результатов анализа
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (var type in fieldsByType.Keys)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(type, EditorStyles.boldLabel);
            
            // Отображение полей
            EditorGUILayout.LabelField("Fields:", EditorStyles.miniBoldLabel);
            foreach (var field in fieldsByType[type])
            {
                EditorGUILayout.LabelField($"- {field}");
            }
            
            // Отображение методов
            if (methodsByType.ContainsKey(type))
            {
                EditorGUILayout.LabelField("Methods:", EditorStyles.miniBoldLabel);
                foreach (var method in methodsByType[type])
                {
                    EditorGUILayout.LabelField($"- {method}");
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void AnalyzeScript()
    {
        fieldsByType.Clear();
        methodsByType.Clear();
        
        if (selectedScript == null) return;
        
        Type scriptType = selectedScript.GetClass();
        if (scriptType == null) return;
        
        // Анализ текущего класса
        AnalyzeType(scriptType, scriptType.Name);
        
        // Анализ родительских классов
        Type baseType = scriptType.BaseType;
        while (baseType != null && baseType != typeof(MonoBehaviour) && baseType != typeof(UnityEngine.Object) && baseType != typeof(System.Object))
        {
            AnalyzeType(baseType, $"Parent: {baseType.Name}");
            baseType = baseType.BaseType;
        }
        
        // Анализ интерфейсов
        foreach (Type interfaceType in scriptType.GetInterfaces())
        {
            AnalyzeType(interfaceType, $"Interface: {interfaceType.Name}");
        }
    }
    
    private void AnalyzeType(Type type, string typeName)
    {
        if (fieldsByType.ContainsKey(typeName)) return;
        
        // Сбор полей
        List<string> fields = new List<string>();
        FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        
        foreach (FieldInfo field in fieldInfos)
        {
            string accessModifier = GetAccessModifier(field);
            string staticModifier = field.IsStatic ? "static " : "";
            fields.Add($"{accessModifier}{staticModifier}{field.FieldType.Name} {field.Name}");
        }
        
        fieldsByType[typeName] = fields;
        
        // Сбор методов
        List<string> methods = new List<string>();
        MethodInfo[] methodInfos = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        
        foreach (MethodInfo method in methodInfos)
        {
            // Пропускаем специальные методы и методы из System.Object
            if (method.IsSpecialName || method.DeclaringType == typeof(System.Object)) continue;
            
            string accessModifier = GetAccessModifier(method);
            string staticModifier = method.IsStatic ? "static " : "";
            
            // Получаем параметры метода
            ParameterInfo[] parameters = method.GetParameters();
            string paramsStr = "";
            for (int i = 0; i < parameters.Length; i++)
            {
                paramsStr += $"{parameters[i].ParameterType.Name} {parameters[i].Name}";
                if (i < parameters.Length - 1) paramsStr += ", ";
            }
            
            methods.Add($"{accessModifier}{staticModifier}{method.ReturnType.Name} {method.Name}({paramsStr})");
        }
        
        methodsByType[typeName] = methods;
    }
    
    private string GetAccessModifier(MemberInfo member)
    {
        if (member is FieldInfo field)
        {
            if (field.IsPublic) return "public ";
            if (field.IsPrivate) return "private ";
            if (field.IsFamily) return "protected ";
            if (field.IsAssembly) return "internal ";
            if (field.IsFamilyOrAssembly) return "protected internal ";
        }
        else if (member is MethodInfo method)
        {
            if (method.IsPublic) return "public ";
            if (method.IsPrivate) return "private ";
            if (method.IsFamily) return "protected ";
            if (method.IsAssembly) return "internal ";
            if (method.IsFamilyOrAssembly) return "protected internal ";
        }
        
        return "";
    }
}