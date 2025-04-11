using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;
using System.Text;


namespace FirUtility
{
    public class TypeAnalyzerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Type selectedType;
        private MonoScript selectedMonoScript;
        private GUIStyle lineStyle;
        private GUIStyle headerButtonStyle;

        private string inheritanceInfo;
        
        private bool groupInfo;
        private bool privateInfo = true;

        public void SetType(Type type, MonoScript mono = null)
        {
            selectedType = type;
            selectedMonoScript = mono;
            
            BuildInheritanceInfo(selectedType);
        }
        
        private void OnEnable()
        {
            lineStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 5, 3, 3),
                margin = new RectOffset(0, 0, 2, 2),
                fixedHeight = 20,
                richText = true
            };
            
            headerButtonStyle = new GUIStyle(GUI.skin.button)
            {
                stretchHeight = true,
                padding = new RectOffset(),
                alignment = TextAnchor.MiddleCenter,
                fixedWidth = 20,
                fixedHeight = 20
            };
        }

        private void OnGUI()
        {
            if (selectedType is null)
                return;
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            HeaderSettings();

            BindingFlags flags = GetFlags();
            
            Fields();
            Properties();
            Constructors();
            Methods();

            EditorGUILayout.EndScrollView();

            void HeaderSettings()
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"<b>{inheritanceInfo}</b>", new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true });
                EditorGUILayout.LabelField(settingsStrig(), new GUIStyle(EditorStyles.wordWrappedLabel) { alignment = TextAnchor.MiddleRight });

                string settingsStrig()
                {
                    if (groupInfo && privateInfo) return "All data, without parents'"; 
                    if (groupInfo) return "Only public data, without parents'";
                    if (privateInfo) return "All data";
                    return "Only public data";
                }
            
                string folderSymbol = groupInfo ? "d_Folder Icon" : "d_TextAsset Icon";
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image),  headerButtonStyle))
                {
                    groupInfo = !groupInfo;
                    Repaint();
                }
                folderSymbol = privateInfo ? "d_VisibilityOn" : "d_VisibilityOff";
                if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image),  headerButtonStyle))
                {
                    privateInfo = !privateInfo;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
            void Fields()
            {
                FieldInfo[] fields = selectedType.GetFields(flags);
                EditorGUILayout.LabelField($"<b>Fields ({fields.Length}):</b>", new GUIStyle(EditorStyles.largeLabel) { richText = true });
                
                if (fields is not null)
                {
                    foreach (FieldInfo field in fields)
                    {
                        string accessModifier = field.IsPublic ? Style.PublicColor() 
                            : field.IsFamily ? Style.PrivateColor("protected")
                            : Style.PrivateColor();
                        string staticModifier = field.IsStatic ? Style.StaticColor(" static") : "";
                        EditorGUILayout.LabelField(
                            $"{accessModifier}{staticModifier} <color=#4EC9B0>{field.FieldType.Name}</color> <color=#DCDCAA>{field.Name}</color>",
                            new GUIStyle(EditorStyles.label) { richText = true });
                    }
                }
            }
            void Properties()
            {
                PropertyInfo[] properties = selectedType.GetProperties(flags);
                EditorGUILayout.LabelField($"<b>Properties ({properties.Length}):</b>", new GUIStyle(EditorStyles.largeLabel) { richText = true });
                if (properties is not null)
                {
                    foreach (PropertyInfo property in properties)
                    {
                        MethodInfo getter = property.GetGetMethod(true);
                        MethodInfo setter = property.GetSetMethod(true);

                        string accessModifier = (getter?.IsPublic ?? false) || (setter?.IsPublic ?? false)
                            ? Style.PublicColor()
                            : (getter?.IsFamily ?? false) || (setter?.IsFamily ?? false) 
                                ? Style.PrivateColor("protected") 
                                : Style.PrivateColor();
                        string staticModifier = (getter?.IsStatic ?? false) ? Style.StaticColor(" static") : "";

                        EditorGUILayout.LabelField(
                            $"{accessModifier}{staticModifier} <color=#4EC9B0>{property.PropertyType.Name}</color> <color=#DCDCAA>{property.Name}</color>",
                            new GUIStyle(EditorStyles.label) { richText = true });
                    }
                }
            }
            void Constructors()
            {
                ConstructorInfo[] constructors = selectedType.GetConstructors(flags);
                EditorGUILayout.LabelField($"<b>Constructors ({constructors.Length}):</b>", new GUIStyle(EditorStyles.largeLabel) { richText = true });
                if (constructors is not null)
                {
                    foreach (ConstructorInfo constructor in constructors)
                    {
                        string accessModifier = constructor.IsPublic ? Style.PublicColor() : 
                            constructor.IsFamily ? Style.PrivateColor("protected") :  
                            Style.PrivateColor();

                        ParameterInfo[] parameters = constructor.GetParameters();
                        string paramsStr = string.Join(", ", parameters.Select(p =>
                            $"<color=#4EC9B0>{p.ParameterType.Name}</color> <color=#9CDCFE>{p.Name}</color>"));

                        string buttonText =
                            $"{accessModifier} <b>{constructor.Name}</b>({paramsStr})";
                        EditorGUILayout.LabelField(buttonText, new GUIStyle(EditorStyles.label) { richText = true });
                    }
                }
            }
            void Methods()
            {
                MethodInfo[] methods = selectedType.GetMethods(flags);
                EditorGUILayout.LabelField($"<b>Methods ({methods.Length}):</b>", new GUIStyle(EditorStyles.largeLabel) { richText = true });
                if (methods is not null)
                {
                    foreach (MethodInfo method in methods)
                    {
                        string accessModifier = method.IsPublic ? Style.PublicColor() 
                            : method.IsFamily ? Style.PrivateColor("protected")
                            : Style.PrivateColor();
                        string staticModifier = method.IsStatic ? Style.StaticColor(" static") : 
                            method.IsAbstract ? Style.StaticColor(" abstract") : 
                            (method.IsVirtual && (method.GetBaseDefinition() != method)) ? Style.StaticColor(" override") :
                            method.IsVirtual ? Style.StaticColor(" virtual") :
                            "";
                        string returnType = $"<color=#4EC9B0>{method.ReturnType.Name}</color>";

                        ParameterInfo[] parameters = method.GetParameters();
                        string paramsStr = string.Join(", ", parameters.Select(p =>
                            $"<color=#4EC9B0>{p.ParameterType.Name}</color> <color=#9CDCFE>{p.Name}</color>"));

                        string buttonText =
                            $"{accessModifier}{staticModifier} {returnType} <b>{method.Name}</b>({paramsStr})";
                        EditorGUILayout.LabelField(buttonText, new GUIStyle(EditorStyles.label) { richText = true });
                    }
                }
            }
            
            BindingFlags GetFlags()
            {
                if(groupInfo && privateInfo) return BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
                if(groupInfo) return BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
                if(privateInfo) return BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
                return BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            }
        }
        
        private void BuildInheritanceInfo(Type type)
        {
            string result = $"{GetTypePrefix(type)}: {type.Name}";
            
            result += Environment.NewLine;
            Type baseType = type.BaseType;
            if (baseType != null)
            {
                result += "Inherits from: ";
                while (baseType != null)
                {
                    result += $"{baseType.FullName}";
                    baseType = baseType.BaseType;
                    if (baseType != null)
                    {
                        result += " → ";
                    }
                }
            }
            
            result += Environment.NewLine;
            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                result += "Implements: ";
                for (int i = 0; i < interfaces.Length; i++)
                {
                    result += interfaces[i].Name;
                    if (i < interfaces.Length - 1) 
                        result += ", ";
                }
            }
            
            inheritanceInfo = $"{result}";
        }
        
        private string GetTypePrefix(Type type)
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
        
        //open code in editor
        private void OpenMethodInIDE(MethodInfo method)
        {
            AssetDatabase.OpenAsset(selectedMonoScript);
            
            string scriptPath = AssetDatabase.GetAssetPath(selectedMonoScript);
            string[] lines = System.IO.File.ReadAllLines(scriptPath);
            
            string methodDeclaration = $" {method.Name}(";
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(methodDeclaration))
                {
                    #if UNITY_2020_1_OR_NEWER
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(scriptPath, i + 1);
                    #endif
                    break;
                }
            }
        }
    }
}