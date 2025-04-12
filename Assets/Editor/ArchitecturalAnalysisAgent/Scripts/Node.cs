using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class Node
    {
        private Vector2 position;
        public Rect rect;
        public string title;
        public int colorIndex;
        
        public bool isDragged;
        public bool isSelected;

        public System.Action<Node> OnEditNode;
        public System.Action<Node> OnRemoveNode;

        public GUIStyle simpleStyle;
        public GUIStyle selectedStyle;

        private NodeMapSettings map;
        
        public Node(string title,
            NodeMapSettings mapSettings,
            Vector2 position = default,
            //System.Action<ConnectionPoint> OnClickInPoint = null, 
            System.Action<Node> OnClickEditNode = null,
            System.Action<Node> OnClickRemoveNode = null)
        {
            this.title = title;
            map = mapSettings;
            this.position = position - map.Offset;
            
            OnEditNode = OnClickEditNode;
            OnRemoveNode = OnClickRemoveNode;

            colorIndex = 1;
            simpleStyle = new GUIStyle(Style.SimpleNode());
            selectedStyle = new GUIStyle(Style.SelectedNode());
        }

        private void Drag(Vector2 delta)
        {
            rect.position += delta;
        }

        public void Draw()
        {
            GUIStyle styleToUse = isSelected ? Style.SelectedNode(colorIndex) : Style.SimpleNode(colorIndex);
            Vector2 textSize = styleToUse.CalcSize(new GUIContent(title));
               
            float width = Mathf.Max(textSize.x + 40, Style.MinButtonWidth) * map.Zoom;
            float height = Style.MinButtonHeight * map.Zoom;

            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            Vector2 resultOffset = (map.Offset + position);
            
            rect = new Rect(resultOffset.x - halfWidth, resultOffset.y - halfHeight, width, height);
            
            GUI.Box(rect, title, styleToUse);
        }

        public bool ProcessEvents(Event e)
        {
            //Vector2 point = e.mousePosition + new Vector2(0, -height);
            
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