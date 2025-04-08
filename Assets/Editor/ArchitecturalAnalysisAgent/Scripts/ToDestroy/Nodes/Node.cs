using UnityEditor;
using UnityEngine;

public class Node
{
    public Rect rect;
    public string title;
    public bool isDragged;
    public bool isSelected;
    
    public ConnectionPoint inPoint;
    public ConnectionPoint outPoint;
    
    public GUIStyle style;
    public GUIStyle defaultNodeStyle;
    public GUIStyle selectedNodeStyle;
    
    public System.Action<Node> OnRemoveNode;
    
    public Node(Vector2 position, float width, float height, GUIStyle nodeStyle, GUIStyle selectedStyle, 
        System.Action<ConnectionPoint> OnClickInPoint, System.Action<ConnectionPoint> OnClickOutPoint, 
        System.Action<Node> OnClickRemoveNode)
    {
        rect = new Rect(position.x, position.y, width, height);
        style = nodeStyle;
        inPoint = new ConnectionPoint(this, ConnectionPointType.In, OnClickInPoint);
        outPoint = new ConnectionPoint(this, ConnectionPointType.Out, OnClickOutPoint);
        defaultNodeStyle = nodeStyle;
        selectedNodeStyle = selectedStyle;
        OnRemoveNode = OnClickRemoveNode;
    }
    
    public void Drag(Vector2 delta)
    {
        rect.position += delta;
    }
    
    public void Draw()
    {
        inPoint.Draw();
        outPoint.Draw();
        GUI.Box(rect, title, style);
        
        // Draw node name field
        Rect nameRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 20);
        title = EditorGUI.TextField(nameRect, title);
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
                        GUI.changed = true;
                        isSelected = true;
                        style = selectedNodeStyle;
                    }
                    else
                    {
                        GUI.changed = true;
                        isSelected = false;
                        style = defaultNodeStyle;
                    }
                }
                
                if (e.button == 1 && isSelected && rect.Contains(e.mousePosition))
                {
                    ProcessContextMenu();
                    e.Use();
                }
                break;
            
            case EventType.MouseUp:
                isDragged = false;
                break;
            
            case EventType.MouseDrag:
                if (e.button == 0 && isDragged)
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
}