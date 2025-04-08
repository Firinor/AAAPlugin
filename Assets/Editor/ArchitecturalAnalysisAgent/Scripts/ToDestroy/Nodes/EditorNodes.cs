using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class NodeEditorWindow : EditorWindow
{
    private List<Node> nodes = new List<Node>();
    private List<Connection> connections = new List<Connection>();
    
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    
    private ConnectionPoint selectedInPoint;
    private ConnectionPoint selectedOutPoint;
    
    private Vector2 offset;
    private Vector2 drag;
    
    [MenuItem("FirUtils/Node Editor")]
    private static void OpenWindow()
    {
        NodeEditorWindow window = GetWindow<NodeEditorWindow>();
        window.titleContent = new GUIContent("Node Editor");
    }
    
    private void OnEnable()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        
        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
    }
    
    private void OnGUI()
    {
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);
        
        DrawNodes();
        DrawConnections();
        
        DrawConnectionLine(Event.current);
        
        ProcessNodeEvents(Event.current);
        ProcessEvents(Event.current);
        
        if (GUI.changed) Repaint();
    }
    
    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);
        
        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
        
        offset += drag * 0.5f;
        Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);
        
        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset,
                new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
        }
        
        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset,
                new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
        }
        
        Handles.color = Color.white;
        Handles.EndGUI();
    }
    
    private void DrawNodes()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Draw();
        }
    }
    
    private void DrawConnections()
    {
        for (int i = 0; i < connections.Count; i++)
        {
            connections[i].Draw();
        }
    }
    
    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;
        
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 1)
                {
                    ProcessContextMenu(e.mousePosition);
                }
                break;
            
            case EventType.MouseDrag:
                if (e.button == 0)
                {
                    OnDrag(e.delta);
                }
                break;
        }
    }
    
    private void ProcessNodeEvents(Event e)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            bool guiChanged = nodes[i].ProcessEvents(e);
            
            if (guiChanged)
            {
                GUI.changed = true;
            }
        }
    }
    
    private void DrawConnectionLine(Event e)
    {
        if (selectedInPoint != null && selectedOutPoint == null)
        {
            Handles.DrawBezier(
                selectedInPoint.rect.center,
                e.mousePosition,
                selectedInPoint.rect.center + Vector2.left * 50f,
                e.mousePosition - Vector2.left * 50f,
                Color.white,
                null,
                2f
            );
            
            GUI.changed = true;
        }
        
        if (selectedOutPoint != null && selectedInPoint == null)
        {
            Handles.DrawBezier(
                selectedOutPoint.rect.center,
                e.mousePosition,
                selectedOutPoint.rect.center - Vector2.left * 50f,
                e.mousePosition + Vector2.left * 50f,
                Color.white,
                null,
                2f
            );
            
            GUI.changed = true;
        }
    }
    
    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition));
        genericMenu.ShowAsContext();
    }
    
    private void OnDrag(Vector2 delta)
    {
        drag = delta;
        
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Drag(delta);
        }
        
        GUI.changed = true;
    }
    
    private void OnClickAddNode(Vector2 mousePosition)
    {
        nodes.Add(new Node(mousePosition, 200, 50, nodeStyle, selectedNodeStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
    }
    
    private void OnClickInPoint(ConnectionPoint inPoint)
    {
        selectedInPoint = inPoint;
        
        if (selectedOutPoint != null)
        {
            if (selectedOutPoint.node != selectedInPoint.node)
            {
                CreateConnection();
                ClearConnectionSelection();
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }
    
    private void OnClickOutPoint(ConnectionPoint outPoint)
    {
        selectedOutPoint = outPoint;
        
        if (selectedInPoint != null)
        {
            if (selectedOutPoint.node != selectedInPoint.node)
            {
                CreateConnection();
                ClearConnectionSelection();
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }
    
    private void OnClickRemoveNode(Node node)
    {
        List<Connection> connectionsToRemove = new List<Connection>();
        
        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].inPoint == node.inPoint || connections[i].outPoint == node.outPoint)
            {
                connectionsToRemove.Add(connections[i]);
            }
        }
        
        for (int i = 0; i < connectionsToRemove.Count; i++)
        {
            connections.Remove(connectionsToRemove[i]);
        }
        
        nodes.Remove(node);
    }
    
    private void CreateConnection()
    {
        connections.Add(new Connection(selectedInPoint, selectedOutPoint, OnClickRemoveConnection));
    }
    
    private void ClearConnectionSelection()
    {
        selectedInPoint = null;
        selectedOutPoint = null;
    }
    
    private void OnClickRemoveConnection(Connection connection)
    {
        connections.Remove(connection);
    }
}