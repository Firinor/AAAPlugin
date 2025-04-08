using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

public class Parents : EditorWindow
{
    private MonoScript selectedScript;
    private Vector2 scrollPosition;
    
    private List<string> fields = new List<string>();
    private List<string> methods = new List<string>();
    private string inheritanceInfo = "";
    
    [MenuItem("FirUtils/Script Analyzer Parents")]
    public static void ShowWindow()
    {
        var window = CreateInstance<Parents>();
        window.titleContent = new GUIContent("Script Analyzer Parents");
        window.Show();
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
        
        // Шапка с информацией о наследовании
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(inheritanceInfo, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // Отображение результатов анализа
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Отображение полей
        EditorGUILayout.LabelField("Fields:", EditorStyles.miniBoldLabel);
        if (fields.Count == 0)
        {
            EditorGUILayout.LabelField("No fields declared in this class");
        }
        else
        {
            foreach (var field in fields)
            {
                EditorGUILayout.LabelField($"- {field}");
            }
        }
        
        EditorGUILayout.Space();
        
        // Отображение методов
        EditorGUILayout.LabelField("Methods:", EditorStyles.miniBoldLabel);
        if (methods.Count == 0)
        {
            EditorGUILayout.LabelField("No methods declared in this class");
        }
        else
        {
            foreach (var method in methods)
            {
                EditorGUILayout.LabelField($"- {method}");
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void AnalyzeScript()
    {
        fields.Clear();
        methods.Clear();
        inheritanceInfo = "";
        
        if (selectedScript == null) return;
        
        Type scriptType = selectedScript.GetClass();
        if (scriptType == null) return;
        
        // Собираем информацию о наследовании для шапки
        BuildInheritanceInfo(scriptType);
        
        // Получаем только элементы, объявленные непосредственно в этом классе
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | 
                          BindingFlags.Instance | BindingFlags.Static | 
                          BindingFlags.DeclaredOnly;
        
        // Сбор полей
        FieldInfo[] fieldInfos = scriptType.GetFields(flags);
        foreach (FieldInfo field in fieldInfos)
        {
            string accessModifier = GetAccessModifier(field);
            string staticModifier = field.IsStatic ? "static " : "";
            fields.Add($"{accessModifier}{staticModifier}{field.FieldType.Name} {field.Name}");
        }
        
        // Сбор методов
        MethodInfo[] methodInfos = scriptType.GetMethods(flags);
        foreach (MethodInfo method in methodInfos)
        {
            // Пропускаем специальные методы
            if (method.IsSpecialName) continue;
            
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
    }
    
    private void BuildInheritanceInfo(Type type)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Class: {type.Name}");
        
        // Добавляем информацию о родительских классах
        Type baseType = type.BaseType;
        if (baseType != null && baseType != typeof(MonoBehaviour) && baseType != typeof(UnityEngine.Object))
        {
            sb.Append("Inherits from: ");
            while (baseType != null && baseType != typeof(MonoBehaviour) && baseType != typeof(UnityEngine.Object) && baseType != typeof(System.Object))
            {
                sb.Append($"{baseType.Name}");
                baseType = baseType.BaseType;
                if (baseType != null && baseType != typeof(MonoBehaviour) && baseType != typeof(UnityEngine.Object) && baseType != typeof(System.Object))
                {
                    sb.Append(" → ");
                }
            }
            sb.AppendLine();
        }
        
        // Добавляем информацию о реализуемых интерфейсах
        Type[] interfaces = type.GetInterfaces();
        if (interfaces.Length > 0)
        {
            sb.Append("Implements: ");
            for (int i = 0; i < interfaces.Length; i++)
            {
                sb.Append(interfaces[i].Name);
                if (i < interfaces.Length - 1) sb.Append(", ");
            }
        }
        
        inheritanceInfo = sb.ToString();
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