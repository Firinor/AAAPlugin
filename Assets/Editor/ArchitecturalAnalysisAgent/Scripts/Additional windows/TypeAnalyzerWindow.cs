using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;

namespace FirUtility
{
    public class TypeAnalyzerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Type selectedType;
        private MonoScript selectedMonoScript;

        private string assemblyInfo;
        private string classInfo;
        private string inheritanceInfo;
        private string interfacesInfo;
        
        private bool groupInfo = true;
        private bool privateInfo = true;

        public void SetType(MonoScript mono)
        {
            selectedMonoScript = mono;
            selectedType = mono.GetType();
            
            BuildInheritanceInfo(selectedType);
        }
        
        public void SetType(Type type)
        {
            selectedType = type;
            
            BuildInheritanceInfo(selectedType);
        }
        private void HandleKeyboardEvents()
        {
            Event currentEvent = Event.current;
            
            //Ctrl+C (Command+C on Mac)
            if (currentEvent.type == EventType.KeyDown 
                && currentEvent.keyCode == KeyCode.C 
                && (currentEvent.control || currentEvent.command))
            {
                CopyClassNameToClipboard();
                currentEvent.Use(); // Marking the event as processed
            }
        }
        private void CopyClassNameToClipboard()
        {
            GUIUtility.systemCopyBuffer = selectedType.Name;
            ShowNotification(new GUIContent($"Copied: {GUIUtility.systemCopyBuffer}"));
            Focus();
        }
        private void OnGUI()
        {
#if !UNITY_2020_1_OR_NEWER
            //Set dark background color
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(.25f, .25f, .25f));
            bgTex.Apply();
            GUI.DrawTexture(new Rect(0, 0, position.width, position.height), bgTex);
#endif
            
            if (selectedType is null)
                return;
            
            EditorGUILayout EditorGUILayout = new();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            HandleKeyboardEvents();
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
                
                EditorGUILayout.Label($"<b>{assemblyInfo}</b>", 
                    new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true });
                
                EditorGUILayout.LabelField(settingsStrig(), 
                    new GUIStyle(EditorStyles.wordWrappedLabel) { alignment = TextAnchor.MiddleRight });

                string settingsStrig()
                {
                    if (groupInfo && privateInfo) return "All data, without parents'"; 
                    if (groupInfo) return "Only public data, without parents'";
                    if (privateInfo) return "All data";
                    return "Only public data";
                }
            
                string folderSymbol = groupInfo ? "d_Folder Icon" : "d_TextAsset Icon";
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image), Style.Button()))
                {
                    groupInfo = !groupInfo;
                    Repaint();
                }
                folderSymbol = privateInfo ? "d_VisibilityOn" : "d_VisibilityOff";
                if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image), Style.Button()))
                {
                    privateInfo = !privateInfo;
                    Repaint();
                }
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Duplicate").image), Style.Button()))
                {
                    CopyClassNameToClipboard();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Label($"<b>{classInfo}</b>", 
                    new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true });
                if(!String.IsNullOrEmpty(inheritanceInfo))
                    EditorGUILayout.Label($"<b>{inheritanceInfo}</b>", 
                        new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true }, false);
                if(!String.IsNullOrEmpty(interfacesInfo))
                    EditorGUILayout.Label($"<b>{interfacesInfo}</b>", 
                        new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true }, false);
            }
            void Fields()
            {
                FieldInfo[] fields = selectedType.GetFields(flags);
                EditorGUILayout.Label($"<b>Fields ({fields.Length}):</b>", 
                    new GUIStyle(EditorStyles.largeLabel) { richText = true });
                
                if (fields != null)
                {
                    foreach (FieldInfo field in fields)
                    {
                        string accessModifier = field.IsPublic ? Style.PublicColor() 
                            : field.IsFamily ? Style.PrivateColor("protected")
                            : Style.PrivateColor();
                        string staticModifier = field.IsStatic ? Style.StaticColor(" static") : "";
                        EditorGUILayout.Label(
                            $"{accessModifier}{staticModifier} <color=#4EC9B0>{field.FieldType}</color> <color=#DCDCAA>{field.Name}</color>",
                            new GUIStyle(EditorStyles.label) { richText = true });
                    }
                }
            }
            void Properties()
            {
                PropertyInfo[] properties = selectedType.GetProperties(flags);
                EditorGUILayout.Label($"<b>Properties ({properties.Length}):</b>", 
                    new GUIStyle(EditorStyles.largeLabel) { richText = true });
                
                if (properties != null)
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

                        EditorGUILayout.Label(
                            $"{accessModifier}{staticModifier} <color=#4EC9B0>{property.PropertyType.Name}</color> <color=#DCDCAA>{property.Name}</color>",
                            new GUIStyle(EditorStyles.label) { richText = true });
                    }
                }
            }
            void Constructors()
            {
                ConstructorInfo[] constructors = selectedType.GetConstructors(flags);
                EditorGUILayout.Label($"<b>Constructors ({constructors.Length}):</b>", 
                    new GUIStyle(EditorStyles.largeLabel) { richText = true });
                
                if (constructors != null)
                {
                    foreach (ConstructorInfo constructor in constructors)
                    {
                        string accessModifier = constructor.IsPublic ? Style.PublicColor() : 
                            constructor.IsFamily ? Style.PrivateColor("protected") :  
                            Style.PrivateColor();

                        ParameterInfo[] parameters = constructor.GetParameters();
                        string paramsStr = string.Join(", ", parameters.Select(parameterInfo =>
                            $"<color=#4EC9B0>{parameterInfo.ParameterType.Name}</color> <color=#9CDCFE>{parameterInfo.Name}</color>"));

                        string text =
                            $"{accessModifier} <b>{constructor}</b>({paramsStr})";
                        EditorGUILayout.Label(text, new GUIStyle(EditorStyles.label) { richText = true });
                    }
                }
            }
            void Methods()
            {
                MethodInfo[] methods = selectedType.GetMethods(flags);
                EditorGUILayout.Label($"<b>Methods ({methods.Length}):</b>", 
                    new GUIStyle(EditorStyles.largeLabel) { richText = true });
                
                if (methods != null)
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
                        string returnType = $"<color=#4EC9B0>{method.ReturnType}</color>";

                        string genericStr = Analyzer.GetMethodPostfix(method);
                        ParameterInfo[] parameters = method.GetParameters();
                        string paramsStr = string.Join(", ", parameters.Select(parameterInfo =>
                            $"<color=#4EC9B0>{parameterInfo.ParameterType}</color> <color=#9CDCFE>{parameterInfo.Name}</color>"));

                        string text =
                            $"{accessModifier}{staticModifier} {returnType} <b>{method.Name}{genericStr}</b>({paramsStr})";
                        EditorGUILayout.Label(text, new GUIStyle(EditorStyles.label) { richText = true });
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
            assemblyInfo = $"{Style.AssemblyColor() + ": "}" +
                           $"{type.Assembly.GetName().Name}" +
                           $"{", version: " + type.Assembly.GetName().Version}";
            
            classInfo =    $"{Analyzer.GetTypePrefix(type)}: {type} " +
                           $"{Environment.NewLine}" +
                           $"{Analyzer.GetTypePostfix(type)}";

            inheritanceInfo = "";
            Type baseType = type.BaseType;
            if (baseType != null)
            {
                inheritanceInfo += Style.StaticColor("Inherits from: ");
                while (baseType != null)
                {
                    inheritanceInfo += $"{baseType}";
                    baseType = baseType.BaseType;
                    if (baseType != null)
                    {
                        inheritanceInfo += " → ";
                    }
                }
            }

            interfacesInfo = "";
            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                interfacesInfo += Style.StaticColor("Implements: ");
                for (int i = 0; i < interfaces.Length; i++)
                {
                    interfacesInfo += interfaces[i].Name;
                    if (i < interfaces.Length - 1) 
                        interfacesInfo += ", ";
                }
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