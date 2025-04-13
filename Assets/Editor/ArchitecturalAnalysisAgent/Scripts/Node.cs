using System;
using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class Node
    {
        private Vector2 position;
        private Rect rect;
        public Type type;
        public string title;
        public int colorIndex = 1;
        
        private bool isDragged;
        private bool isSelected;

        private Action<Node> OnEditNode;
        private Action<Node> OnRemoveNode;

        private NodeMapSettings map;

        public Node(Type type,
            NodeMapSettings mapSettings,
            Vector2 position,
            NodeMapSettings.NodeColor color = NodeMapSettings.NodeColor.Blue) 
            
            : this(type.FullName, mapSettings, position, color)
        {
            this.type = type;
            if (type.IsGenericType)
            {
                title = type.Name;
            }
        }
        
        public Node(string title,
            NodeMapSettings mapSettings,
            Vector2 position,
            NodeMapSettings.NodeColor color = NodeMapSettings.NodeColor.Blue)
        {
            this.title = title;
            map = mapSettings;
            this.position = position / map.Zoom;
            
            OnEditNode = map.OnEditNode;
            OnRemoveNode = map.OnRemoveNode;

            colorIndex = (int)color;
        }

        private void Drag(Vector2 delta)
        {
            position += delta / map.Zoom;
        }

        public void Unselect()
        {
            isSelected = false;
        }
        public void Draw()
        {
            GUIStyle styleToUse = isSelected ? Style.SelectedNode(colorIndex) : Style.SimpleNode(colorIndex);
            Vector2 textSize = styleToUse.CalcSize(new GUIContent(title));
               
            float width = Mathf.Max(textSize.x + 40, Style.MinButtonWidth) ;
            float height =Mathf.Max(textSize.y * map.Zoom, Style.MinButtonHeight);

            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            Vector2 resultOffset = map.Offset + position * map.Zoom;
            
            rect = new Rect(resultOffset.x - halfWidth, resultOffset.y - halfHeight, width, height);
            
            GUI.Box(rect, title, styleToUse);
        }

        public bool ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            isDragged = true;
                            isSelected = true;
                            GUI.changed = true;
                        }
                        else
                        {
                            isSelected = false;
                            GUI.changed = true;
                        }
                    }

                    if (e.button == 1 
                        //&& isSelected 
                        && rect.Contains(e.mousePosition))
                    {
                        ProcessContextMenu();
                        e.Use();
                    }

                    break;

                case EventType.MouseUp:
                    isDragged = false;
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && isDragged && isSelected)
                    {
                        Drag(e.delta);
                        e.Use();
                        return true;
                    }

                    break;
            }

            return false;
        }

        private void ProcessContextMenu()
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Change node"), false, () => OnEditNode?.Invoke(this));
            genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
            genericMenu.ShowAsContext();
        }
        
        private void OnClickRemoveNode()
        {
            if (OnRemoveNode != null)
            {
                OnRemoveNode(this);
            }
        }

        public void Destroy()
        {
            //ConnectionPoint
        }
    }
}