using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class Node
    {
        public Vector2 position;
        public Rect rect;
        public string name;
        public int colorIndex = 1;
        
        private bool isDragged;
        private bool isSelected;

        public HashSet<Node> connections = new HashSet<Node>();

        protected Action<Node> OnRemoveNode;
        
        protected NodeMapSettings map;

        public Node(string name,
            NodeMapSettings mapSettings,
            Vector2 position,
            NodeMapSettings.NodeColor color = NodeMapSettings.NodeColor.Blue)
        {
            this.name = name;
            map = mapSettings;
            this.position = position / map.Zoom;
            
            OnRemoveNode = map.OnRemoveNode;

            colorIndex = (int)color;
        }

        private void Drag(Vector2 delta)
        {
            position += delta / map.Zoom;
        }
        
        public void Draw()
        {
            GUIStyle styleToUse = isSelected ? Style.SelectedNode(colorIndex) : Style.SimpleNode(colorIndex);
            Vector2 textSize = styleToUse.CalcSize(new GUIContent(name));
               
            float width = Mathf.Max(textSize.x + 40, Style.MinButtonWidth) ;
            float height =Mathf.Max(textSize.y * map.Zoom, Style.MinButtonHeight);

            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            Vector2 resultOffset = map.Offset + position * map.Zoom;
            
            rect = new Rect(resultOffset.x - halfWidth, resultOffset.y - halfHeight, width, height);
            
            GUI.Box(rect, name, styleToUse);
        }

        public void ConnectNode(Node target)
        {
            connections.Add(target);
            target.OnRemoveNode += DisconnectNode;
        }

        private void DisconnectNode(Node other)
        {
            connections.Remove(other);
        }

        public void DrawConnections()
        {
            Vector2 Vector2offset = new Vector2(5, 5);
            Node connectionToRemove = null;
            
            foreach (var connection in connections)
            {
                float directionX = connection.position.x - position.x;
                float directionY = connection.position.y - position.y;
                bool isHorizontal = Mathf.Abs(directionX) > Mathf.Abs(directionY);
                if (isHorizontal)
                {
                    directionY = 0;
                    directionX = directionX > 0 ? 1 : -1;
                }
                else
                {
                    directionX = 0;
                    directionY = directionY > 0 ? 1 : -1;
                }

                Vector2 direction = new Vector2(directionX, directionY);
                
                const float arrowSize = 10f;

                Vector2 nodeRectSize = new Vector2(rect.width-arrowSize, rect.height-arrowSize)/2f ;
                Vector2 startPosition = map.Offset + position * map.Zoom + (nodeRectSize-Vector2offset) * direction;
                
                Vector2 connectionNodeRectSize = new Vector2(connection.rect.width-arrowSize, connection.rect.height-arrowSize)/2f;
                Vector2 arrowPosition = map.Offset + connection.position * map.Zoom - (connectionNodeRectSize-Vector2offset) * direction;
                Vector2 endPosition = map.Offset + connection.position * map.Zoom - connectionNodeRectSize * direction;

                Vector2 arrowLeft = endPosition - (Vector2)(Quaternion.Euler(0, 0, -30) * direction * arrowSize);
                Vector2 arrowRight = endPosition - (Vector2)(Quaternion.Euler(0, 0, 30) * direction * arrowSize);

                Handles.DrawBezier(
                    startPosition,
                    endPosition,
                    startPosition + direction * 50f,
                    endPosition - direction * 50f,
                    Color.white,
                    null,
                    4f
                );
                Handles.DrawAAConvexPolygon(arrowPosition, arrowLeft, arrowRight, endPosition);
                
                if (Handles.Button(
                        (startPosition + endPosition) * 0.5f, 
                        Quaternion.identity, 
                        1.2f, 4, 
                        Handles.RectangleHandleCap))
                {
                    connectionToRemove = connection;
                }
            }

            if (connectionToRemove is not null)
                DisconnectNode(connectionToRemove);
        }
        
        public bool ProcessEvents(Event e, bool isCanBeSelected)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (isCanBeSelected && rect.Contains(e.mousePosition))
                        {
                            isDragged = true;
                            isSelected = true;
                            GUI.changed = true;
                            return true;
                        }
                        else if(isSelected)
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
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Delete && isSelected)
                    {
                        OnRemoveNode?.Invoke(this);
                        return true;
                    }
                    break;
            }

            return false;
        }

        protected virtual void ProcessContextMenu()
        {
            GenericMenu genericMenu = new GenericMenu();
            
            genericMenu.AddItem(new GUIContent("Add connection"), false, 
                () => map.OnAddConnection?.Invoke(this));
            genericMenu.AddItem(new GUIContent("Remove connections"), false, 
                () => map.OnRemoveConnections?.Invoke(this));
            genericMenu.AddItem(new GUIContent("Copy name"), false, 
                () => map.OnCopyNode?.Invoke(name.Split('.').Last()));
            genericMenu.AddItem(new GUIContent("Change node"), false, 
                () => map.OnEditNode?.Invoke(this));
            genericMenu.AddItem(new GUIContent("Remove node"), false, 
                () => OnRemoveNode?.Invoke(this));
            
            genericMenu.ShowAsContext();
        }

        public void Destroy()
        {
            OnRemoveNode?.Invoke(this);
        }
    }
}