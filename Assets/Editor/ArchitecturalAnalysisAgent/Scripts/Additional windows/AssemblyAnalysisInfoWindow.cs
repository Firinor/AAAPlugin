using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class AssemblyAnalysisInfoWindow : EditorWindow
    {
        private Assembly _assembly;
        
        public void SetAssembly(Assembly assembly)
        { 
            _assembly = assembly;
        }
        
        private void OnGUI()
        {
            if(_assembly is null) return;
            
            string assemblyPath = _assembly.Location;
            var types = _assembly.GetTypes();
            
            EditorGUILayout.Space();
            DateTime lastModified = File.GetLastWriteTime(assemblyPath);
            GUILayout.Label("Last indexed: " + lastModified, EditorStyles.boldLabel);
            var namespacesCount = types
                .Select(t => t.Namespace)
                .Where(n => n != null)
                .Distinct()
                .Count();
            GUILayout.Label("Namespaces count: " + namespacesCount, EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            int staticClassCount = types.Count(t => t.IsClass && Analyzer.IsStaticClass(t));
            GUILayout.Label("Static classes count: " + staticClassCount, EditorStyles.boldLabel);
            int classCount = types.Count(t => t.IsClass && !Analyzer.IsStaticClass(t));
            GUILayout.Label("Non-static classes count: " + classCount, EditorStyles.boldLabel);
            int interfaceCount = types.Count(t => t.IsInterface);
            GUILayout.Label("Interfaces count: " + interfaceCount, EditorStyles.boldLabel);
            int structCount = types.Count(t => t.IsValueType && !t.IsEnum);
            GUILayout.Label("Structs count: " + structCount, EditorStyles.boldLabel);
            int enumCount = types.Count(t => t.IsEnum);
            GUILayout.Label("Enums count: " + enumCount, EditorStyles.boldLabel);
           
        }
    }
}