using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace FirUtility 
{
    public class StaticAnalyzerWindow: EditorWindow
    {
        [MenuItem("Tools/FirUtility/Static Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<StaticAnalyzerWindow>("Static Analyzer").Show();
        }
        
        private Vector2 scrollPosition;
        
        private readonly Dictionary<Assembly, List<Type>> types = new();
        private readonly Dictionary<Assembly, int> assemblyCount = new();
        private readonly Dictionary<Type, int> typeCount = new();
        private readonly Dictionary<string, bool> assemblyFoldouts = new();
        private readonly Dictionary<string, bool> classFoldouts = new();
        
        private EditorGUILayout EditorGUILayout = new();
        private BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
        
        private void OnEnable()
        {
            var tempAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            IOrderedEnumerable<Assembly> orderedEnumerable = tempAssemblies.OrderBy(a =>
            {
                var assemblyName = a.GetName().Name;
                var prefix = 0;

                if (assemblyName.StartsWith("Unity", StringComparison.OrdinalIgnoreCase))
                    prefix = 1;
                else if (assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                    prefix = 2;

                return (prefix, assemblyName);
            });

            foreach (var assembly in orderedEnumerable)
            {
                if (!CheckStatic(assembly))
                    continue;
                
                string assemblyName = assembly.GetName().Name;
                assemblyFoldouts.TryAdd(assemblyName, false);
                types.Add(assembly, new List<Type>());

                List<Type> typesList = assembly.GetTypes().ToList();
                IOrderedEnumerable<Type> orderedEnumerableType = typesList.OrderBy(t =>
                {
                    var typeName = t.FullName;
                    var prefix = 0;
                    
                    if (t.IsSealed && t.IsAbstract)
                        prefix = 1;
                    else if (t.IsValueType && t.IsEnum)
                        prefix = 2;

                    return (prefix, typeName);
                });

                int assemblyInt = 0;
                foreach (var type in orderedEnumerableType)
                {
                    if(!CheckStatic(type))
                        continue;
                    
                    if(!type.IsPublic)
                        continue;
                    
                    if(type.IsValueType && !type.IsEnum)//struct
                        continue;
                        
                    types[assembly].Add(type);

                    string typeName = type.FullName;
                    classFoldouts.TryAdd(typeName, false);

                    int typeInt = 0;
                    //if (type.IsValueType && type.IsEnum)
                    //    typeInt = type.GetFields(flags).Length;
                    //else
                        typeInt = type.GetFields(flags).Length + type.GetProperties(flags).Length + type.GetMethods(flags).Length;
                    assemblyInt += typeInt;
                    
                    typeCount.Add(type, typeInt);
                }
                
                assemblyCount.Add(assembly, assemblyInt);
            }
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var assembly in types.Keys)
            {
                string assemblyName = assembly.GetName().Name;
                
                assemblyFoldouts[assemblyName] = EditorGUILayout.SelectableFoldout( 
                    assemblyFoldouts[assemblyName], 
                    $"<b>{assemblyName} ({assemblyCount[assembly]})</b>",
                    Style.Foldout()
                );
                
                if (assemblyFoldouts[assemblyName])
                {
                    EditorGUI.indentLevel++;
                    foreach (var type in types[assembly])
                    {
                        string typeName = type.FullName;

                        classFoldouts[typeName] = EditorGUILayout.SelectableFoldout(
                            classFoldouts[typeName],
                            $"{Analyzer.GetTypePrefix(type)}: {type.Name} {Analyzer.GetTypePostfix(type)} " 
                            + (String.IsNullOrEmpty(type.Namespace) ? String.Empty : $"namespace: {type.Namespace}")
                            + $" ({typeCount[type]})",
                            Style.Foldout()
                        );
                        
                        if(classFoldouts[typeName])
                            CreateFoldout(type);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void CreateFoldout(Type type)
        {
            string typeName = type.FullName;
            
            EditorGUI.indentLevel++;

            Analyzer.Fields(type, flags);
            Analyzer.Properties(type, flags);
            Analyzer.Methods(type, flags);
            
            EditorGUI.indentLevel--;
        }

        private bool CheckStatic(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (CheckStatic(type))
                    return true;
            }
            
            return false;
        }
        private bool CheckStatic(Type type)
        {
            if (Analyzer.IsStaticClass(type))
                    return true;
            foreach (var field in type.GetFields(flags))
            {
                if (field.IsStatic)
                    return true;
            }
            foreach (var property in type.GetProperties(flags))
            {
                if (property.GetAccessors(false).Any(m => m.IsStatic))
                    return true;
            }
            foreach (var method in type.GetMethods(flags))
            {
                if (method.IsStatic)
                    return true;
            }

            return false;
        }
    }
}