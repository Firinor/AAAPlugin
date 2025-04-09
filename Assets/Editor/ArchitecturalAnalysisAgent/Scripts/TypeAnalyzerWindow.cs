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
        private GUIStyle methodButtonStyle;
        private GUIStyle headerButtonStyle;
        
        private bool groupInfo;
        private bool privateInfo;

        public void SetType(Type type)
        {
            selectedType = type;
        }
        
        private void OnEnable()
        {
            methodButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 5, 3, 3),
                margin = new RectOffset(0, 0, 2, 2),
                fixedHeight = 22,
                richText = true
            };
            
            headerButtonStyle = new GUIStyle(GUI.skin.button);
            headerButtonStyle.stretchHeight = true;
            headerButtonStyle.padding = new RectOffset();
            headerButtonStyle.alignment = TextAnchor.MiddleCenter;
            headerButtonStyle.fixedWidth = 20;
            headerButtonStyle.fixedHeight = 20;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"<b>Type:</b> <color=#569CD6>{selectedType.Name}</color>", new GUIStyle(EditorStyles.label) { richText = true });

            string folderSymbol = groupInfo ? "d_Folder Icon" : "d_TextAsset Icon";
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image),  headerButtonStyle))
            {
                groupInfo = !groupInfo;
            }
            folderSymbol = privateInfo ? "d_VisibilityOn" : "d_VisibilityOff";
            if (GUILayout.Button( new GUIContent(EditorGUIUtility.IconContent(folderSymbol).image),  headerButtonStyle))
            {
                privateInfo = !privateInfo;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("<b>Fields:</b>", new GUIStyle(EditorStyles.label) { richText = true });
            FieldInfo[] fields = selectedType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                string accessModifier = field.IsPublic ? "<color=#569CD6>public</color>" : "<color=#9CDCFE>private</color>";
                string staticModifier = field.IsStatic ? " <color=#DCDCAA>static</color>" : "";
                EditorGUILayout.LabelField($"{accessModifier}{staticModifier} <color=#4EC9B0>{field.FieldType.Name}</color> <color=#DCDCAA>{field.Name}</color>", 
                    new GUIStyle(EditorStyles.label) { richText = true });
            }

            // Свойства класса
            EditorGUILayout.LabelField("<b>Properties:</b>", new GUIStyle(EditorStyles.label) { richText = true });
            PropertyInfo[] properties = selectedType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (PropertyInfo property in properties)
            {
                MethodInfo getter = property.GetGetMethod(true);
                MethodInfo setter = property.GetSetMethod(true);
                
                string accessModifier = (getter?.IsPublic ?? false) || (setter?.IsPublic ?? false) ? "<color=#569CD6>public</color>" : "<color=#9CDCFE>private</color>";
                string staticModifier = (getter?.IsStatic ?? false) ? " <color=#DCDCAA>static</color>" : "";
                
                EditorGUILayout.LabelField($"{accessModifier}{staticModifier} <color=#4EC9B0>{property.PropertyType.Name}</color> <color=#DCDCAA>{property.Name}</color>", 
                    new GUIStyle(EditorStyles.label) { richText = true });
            }

            // Методы класса
            EditorGUILayout.LabelField("<b>Methods:</b>", new GUIStyle(EditorStyles.label) { richText = true });
            MethodInfo[] methods = selectedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => !m.IsSpecialName).ToArray();

            foreach (MethodInfo method in methods)
            {
                string accessModifier = method.IsPublic ? "<color=#569CD6>public</color>" : "<color=#9CDCFE>private</color>";
                string staticModifier = method.IsStatic ? " <color=#DCDCAA>static</color>" : "";
                string returnType = $"<color=#4EC9B0>{method.ReturnType.Name}</color>";

                // Параметры метода
                ParameterInfo[] parameters = method.GetParameters();
                string paramsStr = string.Join(", ", parameters.Select(p => 
                    $"<color=#4EC9B0>{p.ParameterType.Name}</color> <color=#9CDCFE>{p.Name}</color>"));

                // Создаем кнопку для метода
                string buttonText = $"{accessModifier}{staticModifier} {returnType} <b>{method.Name}</b>({paramsStr})";
                if (GUILayout.Button(buttonText, methodButtonStyle))
                {
                    //OpenMethodInIDE(method);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /*private void OpenMethodInIDE(MethodInfo method)
        {
            // Открываем скрипт
            AssetDatabase.OpenAsset(selectedType);
            
            // Пытаемся найти метод в файле
            string scriptPath = AssetDatabase.GetAssetPath(selectedType);
            string[] lines = System.IO.File.ReadAllLines(scriptPath);
            
            // Ищем метод по сигнатуре (более точный поиск)
            string methodDeclaration = $" {method.Name}(";
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(methodDeclaration))
                {
                    // Для Unity 2020.1 и новее
                    #if UNITY_2020_1_OR_NEWER
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(scriptPath, i + 1);
                    #endif
                    break;
                }
            }
        }*/
    }
}