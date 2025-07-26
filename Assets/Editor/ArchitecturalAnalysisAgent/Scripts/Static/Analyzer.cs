using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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

            if(assembly is null) return false;
            
            return true;
        }

        public static Assembly GetAssemblyByDefinition(AssemblyDefinitionAsset assemblyDefinitionAsset)
        {
            if(assemblyDefinitionAsset is null) return null;
            
            string json = assemblyDefinitionAsset.text;
            AssemblyDefinition asmDef = JsonUtility.FromJson<AssemblyDefinition>(json);
            string assemblyName = asmDef.name;
            if (GetAssemblyByName(out Assembly assembly, assemblyName))
                return assembly;
            return null;
        }
        
        public static Dictionary<AssemblyBindData, List<AssemblyBindData>> FindAssemblyReferences()
        {
            Dictionary<AssemblyBindData, List<AssemblyBindData>> result = new();
            
            var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" });
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssemblyDefinitionAsset assemblyDefinitionAsset
                    = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assetPath);
                string json = assemblyDefinitionAsset.text;
                AssemblyDefinition asmDef = JsonUtility.FromJson<AssemblyDefinition>(json);
                Assembly mainAssembly = GetAssemblyByDefinition(assemblyDefinitionAsset);

                AssemblyBindData KeyData = new()
                {
                    Assembly = mainAssembly,
                    AssemblyDefinitionAsset = assemblyDefinitionAsset
                };
                
                List<AssemblyBindData> dictionaryValue = new();
                result.Add(KeyData, dictionaryValue);
                
                if(asmDef.references is null || asmDef.references.Length < 1) continue;
                
                foreach (string reference in asmDef.references)
                {
                    string referenceGuid = reference;
                    if (reference.StartsWith("GUID:"))
                        referenceGuid = reference.Substring(5);
                    string asmDefPath = AssetDatabase.GUIDToAssetPath(referenceGuid);
                    assemblyDefinitionAsset
                        = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmDefPath);
                    Assembly assembly = GetAssemblyByDefinition(assemblyDefinitionAsset);
                    if (assembly is not null)
                    {
                        AssemblyBindData ValueData = new()
                        {
                            Assembly = assembly,
                            AssemblyDefinitionAsset = assemblyDefinitionAsset
                        };
                        dictionaryValue.Add(ValueData);
                    }
                }
            }

            if(result.Count > 0)
                return result;

            return null;
        }

        public static List<Assembly> GetAssemblyByAssets(bool addDefaultAssembly = true)
        {
            List<Assembly> result = new();
            Assembly assembly = null;
            var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets" });

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssemblyDefinitionAsset assemblyDefinitionAsset
                    = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(assetPath);
                assembly = GetAssemblyByDefinition(assemblyDefinitionAsset);
                if (assembly is not null)
                    result.Add(assembly);
            }

            if (addDefaultAssembly)
            {
                if (GetAssemblyByName(out assembly, "Assembly-CSharp")) result.Add(assembly);
                if (GetAssemblyByName(out assembly, "Assembly-CSharp-Editor")) result.Add(assembly);
                if (GetAssemblyByName(out assembly, "Assembly-CSharp-firstpass")) result.Add(assembly);
                if (GetAssemblyByName(out assembly, "Assembly-CSharp-Editor-firstpass")) result.Add(assembly);
                if (GetAssemblyByName(out assembly, "Assembly-CSharp-Test")) result.Add(assembly);
                if (GetAssemblyByName(out assembly, "Assembly-CSharp-Editor-Tests")) result.Add(assembly);
            }

            return result;
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
                    List<Type> types = new List<Type>();

                    foreach (var assemblyObject in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        types.AddRange(assemblyObject.GetTypes()
                            .Where(a => a.ToString() == typeName)
                            .ToArray());
                    }

                    if (types.Count == 1)
                    {
                        type = types[0];
                        return true;
                    }

                    if (types.Count > 1)
                    {
                        type = types[0];
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
                    .FirstOrDefault(a => a.ToString() == typeName);
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
            
            bool IsRecord(Type checkType)
            {
                return checkType.GetMethods().Any(m => m.Name == "<Clone>$");
            }
        }
        public static string GetTypePostfix(Type type)
        {
            if(type is null) 
                return string.Empty;

            bool isResultEmpty = true;
            
            StringBuilder result = new StringBuilder(" <");
            
            if (type.IsGenericType)
            {
                var typesEnum = type.GetGenericArguments().GetEnumerator();
                bool first = true;
                while (typesEnum.MoveNext())
                {
                    Type nextGenericType = typesEnum.Current as Type;

                    bool firstWhereType = true;
                    string localResult = "";
                    
                    if (nextGenericType.BaseType != null
                        && nextGenericType.BaseType != typeof(Object))
                    {
                        localResult = Style.ClassColor(nextGenericType.ToString()) + " is " + nextGenericType.BaseType;
                        firstWhereType = false;
                        isResultEmpty = false;
                    }
                    
                    var genericInterfaces = nextGenericType.GetInterfaces();
                    if (genericInterfaces != null && genericInterfaces.Length > 0)
                    {
                        foreach (var genericInterface in genericInterfaces)
                        {
                            if (firstWhereType)
                            {
                                localResult = Style.ClassColor(nextGenericType.ToString()) + " is ";
                                firstWhereType = false;
                            }
                            else
                                localResult += " and ";
                        
                            localResult += genericInterface;
                            isResultEmpty = false;
                        }
                    }
                    
                    if (!String.IsNullOrEmpty(localResult))
                    {
                        if (!first)
                            result.Append("," + Environment.NewLine);
                        result.Append(localResult);
                        first = false;
                    }
                }
            }

            if (isResultEmpty)
                return string.Empty;
            
            result.Append(">");
            return result.ToString();
        }
        public static string GetMethodPostfix(MethodInfo info)
        {
            var types = info.GetGenericArguments();
            
            if(types is null || types.Length == 0) 
                return string.Empty;
            
            bool isResultEmpty = true;
            
            StringBuilder result = new StringBuilder("<");
            
            bool first = true;
            foreach (Type type in types)
            {
                if(!first)
                    result.Append(", ");

                first = false;
                
                result.Append(type);
                
                bool firstWhereType = true;
                    
                if (type.BaseType != null
                    && type.BaseType != typeof(Object))
                {
                    result.Append(" is " + type.BaseType);
                    firstWhereType = false;
                    isResultEmpty = false;
                }
                    
                var genericInterfaces = type.GetInterfaces();
                if (genericInterfaces != null && genericInterfaces.Length > 0)
                {
                    foreach (var genericInterface in genericInterfaces)
                    {
                        if (firstWhereType)
                        {
                            result.Append(" is ");
                            firstWhereType = false;
                        }
                        else
                            result.Append(" and ");
                        
                        result.Append(genericInterface);
                        isResultEmpty = false;
                    }
                }
            }

            if (isResultEmpty)
                return string.Empty;
            
            result.Append(">");
            return result.ToString();
        }

        public static void ClearTypes(HashSet<Type> usingTypes)
        {
            usingTypes.Remove(null);
            usingTypes.Remove(typeof(void));
            usingTypes.Remove(typeof(string));
            usingTypes.Remove(typeof(int));
            usingTypes.Remove(typeof(float));
            usingTypes.Remove(typeof(bool));
            usingTypes.Remove(typeof(Object));
            
            //MonoBehaviour attributes
            //usingTypes.Remove(typeof(ExtensionsOf));
        }

        public static IEnumerable<Type> GetAllGeneric(Type type)
        {
            if(type is null) 
                yield break;
            
            yield return type;
            
            if (type.IsGenericType)
            {
                var typesEnum = type.GetGenericArguments().GetEnumerator();
                while (typesEnum.MoveNext())
                {
                    Type nextGenericType = typesEnum.Current as Type;
                    yield return nextGenericType;
                    yield return nextGenericType.BaseType;
                }
            }

            if (!type.IsGenericParameter) 
                yield break;
            
            var genericInterfaces = type.GetInterfaces();
            if (genericInterfaces is null || genericInterfaces.Length <= 0) 
                yield break;
            
            foreach (var genericInterface in genericInterfaces)
            {
                yield return genericInterface;
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

            HashSet<Type> types = new HashSet<Type>();

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

            HashSet<Type> types = new HashSet<Type>();
            
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
                var attributes = type.GetCustomAttributes();
                foreach (var info in GetRequireComponentTypes(attributes))
                    foreach (var foundedType in GetAllGeneric(info))
                        if (type.IsAssignableFrom(foundedType)) return true;
                    
                foreach (var info in assemblyType.GetGenericArguments())
                    foreach (var foundedType in GetAllGeneric(info))
                       if (type.IsAssignableFrom(foundedType))
                            return true;
               
                foreach (var info in assemblyType.GetFields(AllBindingFlags))
                    foreach (var foundedType in GetAllGeneric(info.FieldType))
                        if (type.IsAssignableFrom(foundedType)) return true;
            
                foreach (var info in assemblyType.GetProperties(AllBindingFlags))
                    foreach (var foundedType in GetAllGeneric(info.PropertyType))
                        if (type.IsAssignableFrom(foundedType)) return true;
                
                foreach (var constructor in assemblyType.GetConstructors(AllBindingFlags))
                    foreach (var parameterInfo in constructor.GetParameters())
                        foreach (var foundedType in GetAllGeneric(parameterInfo.ParameterType))
                            if (type.IsAssignableFrom(foundedType)) return true;
                
                foreach (var method in assemblyType.GetMethods(AllBindingFlags))
                {
                    foreach (var foundedType in GetAllGeneric(method.ReturnType))
                        if (type.IsAssignableFrom(foundedType)) return true;
                            
                    foreach (var parameterInfo in method.GetParameters())
                        foreach (var foundedType in GetAllGeneric(parameterInfo.ParameterType))
                            if (type.IsAssignableFrom(foundedType)) return true;
                }

                return false;
            }
        }
        
        public static void ShowAssetsScriptsInfo()
        {
            var analysisInfoWindow = EditorWindow.CreateInstance<AssemblyAnalysisInfoWindow>();
            analysisInfoWindow.SetAssembly(GetAssemblyByAssets());
            analysisInfoWindow.titleContent = new GUIContent("Quick Project Analysis");
            analysisInfoWindow.Show();
        }
        public static void ShowAssemblyInfo(AssemblyDefinitionAsset assemblyDefinitionAsset)
        {
            Assembly assembly = GetAssemblyByDefinition(assemblyDefinitionAsset);
            ShowAssemblyInfo(assembly.GetName().Name);
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
        public static void ShowScriptInfo(MonoScript monoScript)
        {
            Type type = monoScript.GetClass();
            if (monoScript.GetClass() is null)
            {
                Debug.LogError("Couldn't detect type inside the file " + monoScript.name + "!");
                return;
            }
            
            TypeAnalyzerWindow analysisInfoWindow = EditorWindow.CreateInstance<TypeAnalyzerWindow>();
            analysisInfoWindow.SetType(type);
            analysisInfoWindow.titleContent = new GUIContent(type.Name + " info");
            analysisInfoWindow.Show();
        }
        public static void ShowScriptInfo(Type type)
        {
            TypeAnalyzerWindow analysisInfoWindow = EditorWindow.CreateInstance<TypeAnalyzerWindow>();
            analysisInfoWindow.SetType(type);
            analysisInfoWindow.titleContent = new GUIContent(type.Name + " info");
            analysisInfoWindow.Show();
        }

        public static void ClearAttributes(HashSet<Attribute> attributes)
        {
            HashSet<Attribute> blackList = new HashSet<Attribute>();
            
            foreach (var attribute in attributes)
            {
                //Default MonoBehaviour and ScriptableObject attributes not interesting for us
                if(attribute.ToString() == "UnityEngine.Bindings.NativeHeaderAttribute")
                    blackList.Add(attribute);
                else if(attribute.ToString() == "UnityEngine.ExtensionOfNativeClassAttribute")
                    blackList.Add(attribute);
            }

            attributes.ExceptWith(blackList);
        }
        
        public static IEnumerable<Type> GetRequireComponentTypes(IEnumerable<Attribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute is RequireComponent requireComponent)
                {
                    yield return requireComponent.m_Type0;
                    yield return requireComponent.m_Type1;
                    yield return requireComponent.m_Type2;
                }
            }
        }
        
        public static bool IsStaticClass(Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }
    }
}