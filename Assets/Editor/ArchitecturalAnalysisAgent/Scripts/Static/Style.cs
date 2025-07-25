using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public static class Style
    {
        private static List<GUIStyle> nodeStyle;
        private static List<GUIStyle> selectedStyle;  
        private static GUIStyle buttonStyle;
        
        public const int MinButtonWidth = 60;
        public const int MinButtonHeight = 50;
        
        public static string AssemblyColor(string text = "Assembly") => $"<color=#DD00DD>{text}</color>";
        public static string PublicColor(string text = "public") => $"<color=#9CDCFE>{text}</color>";
        public static string PrivateColor(string text = "private") => $"<color=#569CD6>{text}</color>";
        public static string StaticColor(string text = "static") => $"<color=#007FFF>{text}</color>";
        public static string ClassColor(string text = "class") => $"<color=#569CD6>{text}</color>";
        public static string InterfaceColor(string text = "interface") => $"<color=#DCDCAA>{text}</color>";
        
        public static GUIStyle SimpleNode(int colorIndex = 1)
        {
            if (nodeStyle is null || nodeStyle.Count == 0)
            {
                nodeStyle = new List<GUIStyle>();

                for (int i = 0; i < 7; i++)
                {
                    nodeStyle.Add(new GUIStyle
                    {
                        normal =
                        {
                            background =
                                EditorGUIUtility.Load($"builtin skins/darkskin/images/node{i}.png") as Texture2D,
                            textColor = Color.white
                        },
                        border = new RectOffset(12, 12, 12, 12),
                        alignment = TextAnchor.MiddleCenter
                    });
                }
            }

            return nodeStyle[colorIndex];
        }
        public static GUIStyle SelectedNode(int colorIndex = 1)
        {
            if (selectedStyle is null || selectedStyle.Count == 0)
            {
                selectedStyle = new List<GUIStyle>();

                for (int i = 0; i < 7; i++)
                {
                    selectedStyle.Add(new GUIStyle
                    {
                        normal =
                        {
                            background = EditorGUIUtility.Load($"builtin skins/darkskin/images/node{i} on.png") as Texture2D,
                            textColor = Color.white
                        },
                        border = new RectOffset(12, 12, 12, 12),
                        alignment = TextAnchor.MiddleCenter
                    });
                }
            }

            return selectedStyle[colorIndex];
        }
        public static GUIStyle Button()
        {
            if (buttonStyle is null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    stretchHeight = true,
                    padding = new RectOffset(),
                    alignment = TextAnchor.MiddleCenter,
                    fixedWidth = 20,
                    fixedHeight = 20
                };
            }

            return buttonStyle;
        }

        public static NodeMapSettings.NodeColor GetColorByType(Type type)
        {
            if (type.IsEnum)
                return NodeMapSettings.NodeColor.Yellow;
            else if(type.IsValueType && !type.IsEnum)
                return NodeMapSettings.NodeColor.Green;
            else if (type.IsInterface)
                return NodeMapSettings.NodeColor.Orange;
            else if (typeof(Delegate).IsAssignableFrom(type))
                return NodeMapSettings.NodeColor.Red;
            else if (type.IsClass)
                return NodeMapSettings.NodeColor.Blue;
            
            return NodeMapSettings.NodeColor.Grey;
        }
    }
}