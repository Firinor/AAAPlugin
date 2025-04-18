using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = System.Object;

namespace FirUtility
{
    public static class Analyzer
    {
        public static bool GetTypeByName(out Type type, string typeName, string assemblyName)
        {
            GetAssemblyByName(out Assembly assembly, assemblyName);

            return GetTypeByName(out type, typeName, assembly);
        }

        public static bool GetAssemblyByName(out Assembly assembly, string assemblyName)
        {
            assembly = null;
            
            try
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            return true;
        }
        
        public static bool GetTypeByName(out Type type, string typeName, Assembly assembly = null)
        {
            type = null;
            
            if (String.IsNullOrEmpty(typeName))
            {
                Debug.LogError("Empty script during analysis");
                return false;
            }

            if (assembly is null)
            {
                try
                {
                    List<Type> types = new();

                    foreach (var assemblyObject in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        types.AddRange(assemblyObject.GetTypes()
                            .Where(a => a.Name == typeName)
                            .ToArray());
                    }

                    if (types.Count == 1)
                    {
                        type = types[0];
                        return true;
                    }

                    if (types.Count > 1)
                    {
                        Debug.LogError("Found more than 1 type with a matching name");
                    }
                    if (types.Count < 1)
                    {
                        Debug.LogError("No suitable type was found");
                    }

                    return false;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return false;
                }
            }

            try
            {
                type = assembly.GetTypes()
                    .FirstOrDefault(a => a.FullName == typeName);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            
            if (type is null)
            {
                Debug.LogError("Null type during analysis");
                return false;
            }

            return true;
        }
        
        public static string GetTypePrefix(Type type)
        {
            return GetPublicity() + GetStatic() + " " + GetGetTypePrefix();

            string GetPublicity()
            {
                if (type.IsPublic) return Style.PublicColor();
                if (type.IsNestedPublic) return Style.PublicColor();
                if (type.IsNestedFamily) return Style.PrivateColor("protected");
                if (type.IsNestedAssembly) return Style.PublicColor("internal");
                if (type.IsNestedFamORAssem) return "protected internal";
                if (type.IsNestedFamANDAssem) return "private protected";
                if (type.IsNotPublic) return Style.PublicColor("internal");
                if (type.IsNestedPrivate) return Style.PrivateColor();
                return "unknown";
            }

            string GetStatic()
            {
                if (type.IsSealed && type.IsAbstract)
                    return Style.StaticColor(" static");
                if(type.IsAbstract)
                    return Style.StaticColor(" abstract");
                return String.Empty;
            }
            
            string GetGetTypePrefix()
            {
                string result = null;
                if (type.IsClass) result = IsRecord(type) ? "record (class)" : "class";
                if (!String.IsNullOrEmpty(result))
                    return Style.ClassColor(result);
                
                if (type.IsValueType && !type.IsEnum) result = IsRecord(type) ? "record struct" : "struct";
                else if (type.IsEnum) result = "enum";
                else if (type.IsInterface) result = "interface";
                if (!String.IsNullOrEmpty(result))
                    return Style.InterfaceColor(result);
                
                return "unknown";
            }
            
            bool IsRecord(Type type)
            {
                return type.GetMethods().Any(m => m.Name == "<Clone>$");
            }
        }

        public static void ClearCommonTypes(HashSet<Type> usingTypes)
        {
            usingTypes.Remove(typeof(void));
            usingTypes.Remove(typeof(string));
            usingTypes.Remove(typeof(int));
            usingTypes.Remove(typeof(float));
            usingTypes.Remove(typeof(bool));
            usingTypes.Remove(typeof(Object));
        }

        public static IEnumerable<Type> GetAllGeneric(Type type)
        {
            yield return type;
            if (type.IsGenericType)
            {
                var typesEnum = type.GetGenericArguments().GetEnumerator();
                while (typesEnum.MoveNext())
                {
                    yield return typesEnum.Current as Type;
                }
            }
        }
        
        public static BindingFlags AllBindingFlags =>
            BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        public static HashSet<Type> GetAllInheritorOfType(Type type)
        {
            if (type is null)
                return null;
            
            if (type.IsSealed)
                return null;

            HashSet<Type> types = new();

            if (type.IsInterface)
            {
                foreach (var assemblyObject in AppDomain.CurrentDomain.GetAssemblies())
                {
                    types.UnionWith(assemblyObject.GetTypes()
                        .Where(a => 
                            a.GetInterfaces().Contains(type))
                        .ToArray());
                }
            }
            else
            {
                foreach (var assemblyObject in AppDomain.CurrentDomain.GetAssemblies())
                {
                    types.UnionWith(assemblyObject.GetTypes()
                        .Where(type.IsAssignableFrom
                        /*{
                            if() return true;
                            Type parent = a.BaseType;
                            while (parent != null)
                            {
                                if (parent == type) return true;
                                parent = parent.BaseType;
                            }

                            return false;
                        }*/
                        )
                        .ToArray());
                }
            }
            types.Remove(type);
            return types;
        }
        
        public static HashSet<Type> GetAllUsagersOfType(Type type)
        {
            if (type is null)
                return null;

            HashSet<Type> types = new();
            
            foreach (var assemblyObject in AppDomain.CurrentDomain.GetAssemblies())
            {
                if(assemblyObject.FullName.StartsWith("System")
                    || assemblyObject.FullName.StartsWith("UnityEngine")
                    || assemblyObject.FullName.StartsWith("UnityEditor"))
                    continue;
                
                foreach (var assemblyType in assemblyObject.GetTypes())
                {
                    if(assemblyType.FullName.StartsWith("System")
                       || assemblyType.FullName.StartsWith("UnityEngine")
                       || assemblyType.FullName.StartsWith("UnityEditor"))
                        continue;

                    if (FindInType(assemblyType))
                    {
                        types.Add(assemblyType);
                    };
                }
            }

            types.Remove(type);
            
            return types;
            
            bool FindInType(Type assemblyType)
            {
                foreach (var info in assemblyType.GetFields(AllBindingFlags))
                {
                    foreach (var foundedType in GetAllGeneric(info.FieldType))
                    {
                        if (type.IsAssignableFrom(foundedType))
                        {
                            return true;
                        }
                    }
                }
                foreach (var info in assemblyType.GetProperties(AllBindingFlags))
                {
                    foreach (var foundedType in GetAllGeneric(info.PropertyType))
                    {
                        if (type.IsAssignableFrom(foundedType))
                        {
                            return true;
                        }
                    }
                }
                foreach (var constructor in assemblyType.GetConstructors(AllBindingFlags))
                {
                    foreach (var parameterInfo in constructor.GetParameters())
                    {
                        foreach (var foundedType in GetAllGeneric(parameterInfo.ParameterType))
                        {
                            if (type.IsAssignableFrom(foundedType))
                            {
                                return true;
                            }
                        }
                    }
                }
                foreach (var method in assemblyType.GetMethods(AllBindingFlags))
                {
                    foreach (var foundedType in GetAllGeneric(method.ReturnType))
                    {
                        if (type.IsAssignableFrom(foundedType))
                        {
                            return true;
                        }
                    }
                    
                    foreach (var parameterInfo in method.GetParameters())
                    {
                        foreach (var foundedType in GetAllGeneric(parameterInfo.ParameterType))
                        {
                            if (type.IsAssignableFrom(foundedType))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }
        public static void ShowAssemblyInfo(AssemblyDefinitionAsset assemblyDefinitionAsset)
        {
            ShowAssemblyInfo(assemblyDefinitionAsset?.name);
        }
        public static void ShowAssemblyInfo(string assemblyName)
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
                Debug.LogError("Assembly analysis failed!");
            }
            else
            {
                var analysisInfoWindow = EditorWindow.CreateInstance<AssemblyAnalysisInfoWindow>();
                analysisInfoWindow.SetAssembly(assembly);
                analysisInfoWindow.titleContent = new GUIContent("Assembly: " + assembly.GetName().Name + " info");
                analysisInfoWindow.Show();
            }
        }
        public static void ShowScriptInfo(string typeName, string assemblyName = null)
        {
            if (!GetTypeByName(out Type type, typeName, assemblyName)) return;

            ShowScriptInfo(type);
        }
        public static void ShowScriptInfo(Type type)
        {
            TypeAnalyzerWindow analysisInfoWindow = EditorWindow.CreateInstance<TypeAnalyzerWindow>();
            analysisInfoWindow.SetType(type);
            analysisInfoWindow.titleContent = new GUIContent(type.Name + " info");
            analysisInfoWindow.Show();
        }
    }
}