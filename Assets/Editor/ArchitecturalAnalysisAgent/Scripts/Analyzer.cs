using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static void CleareCommonTypes(HashSet<Type> usingTypes)
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
    }
}