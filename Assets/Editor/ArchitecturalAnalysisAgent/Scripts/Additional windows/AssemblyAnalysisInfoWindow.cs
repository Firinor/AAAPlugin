using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class AssemblyAnalysisInfoWindow : EditorWindow
    {
        private List<Assembly> _assemblies;
        
        public void SetAssembly(Assembly assembly)
        { 
            _assemblies = new(){assembly};
        }
        public void SetAssembly(List<Assembly> assemblies)
        { 
            _assemblies = assemblies;
        }
        
        private void OnGUI()
        {
            if(_assemblies is null) return;

            DateTime lastModified = DateTime.MinValue;
            int namespacesCount = 0;
            int staticClassCount = 0;
            int monobehaviourCount = 0;
            int classCount = 0;
            int interfaceCount = 0;
            int structCount = 0;
            int enumCount = 0;

            Type MonoBehaviourType = typeof(MonoBehaviour);
            
            foreach (Assembly assembly in _assemblies)
            {
                string assemblyPath = assembly.Location;
                var types = assembly.GetTypes();
                DateTime modifiedDate = File.GetLastWriteTime(assemblyPath);
                lastModified = lastModified > modifiedDate ? lastModified : modifiedDate;
                namespacesCount += types
                    .Select(t => t.Namespace)
                    .Where(n => n != null)
                    .Distinct()
                    .Count();
                staticClassCount += types.Count(t => t.IsClass && Analyzer.IsStaticClass(t));
                monobehaviourCount += types.Count(t => t.IsClass && !Analyzer.IsStaticClass(t) && MonoBehaviourType.IsAssignableFrom(t));
                classCount += types.Count(t => t.IsClass && !Analyzer.IsStaticClass(t) && !MonoBehaviourType.IsAssignableFrom(t));
                interfaceCount += types.Count(t => t.IsInterface);
                structCount += types.Count(t => t.IsValueType && !t.IsEnum);
                enumCount += types.Count(t => t.IsEnum);
            }
            
            EditorGUILayout EditorGUILayout = new();
            EditorGUILayout.Space();
            EditorGUILayout.Label("Last indexed: " + lastModified);
            EditorGUILayout.Label("Namespaces count: " + namespacesCount);
            EditorGUILayout.Space();
            EditorGUILayout.Label("Static classes count: " + staticClassCount);
            EditorGUILayout.Label("MonoBehaviour classes count: " + monobehaviourCount);
            EditorGUILayout.Label("Other classes count: " + classCount);
            EditorGUILayout.Label("Interfaces count: " + interfaceCount);
            EditorGUILayout.Label("Structs count: " + structCount);
            EditorGUILayout.Label("Enums count: " + enumCount);
        }
    }
}