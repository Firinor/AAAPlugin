namespace FirUtility.ToDestroy
{
    using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AdvancedNodeEditorWindow : EditorWindow
{
    private List<Node> nodes = new List<Node>();
    private List<Connection> connections = new List<Connection>();
    
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    
    private Vector2 offset;
    private Vector2 drag;
    private float zoom = 1.0f;
    
    private Node selectedNodeForEditing;
    private string newNodeName = "";
    private Color newNodeColor = Color.white;
    
    [MenuItem("Window/Advanced Node Editor")]
    private static void OpenWindow()
    {
        AdvancedNodeEditorWindow window = GetWindow<AdvancedNodeEditorWindow>();
        window.titleContent = new GUIContent("Advanced Node Editor");
        window.minSize = new Vector2(800, 600);
    }
    
    private void OnEnable()
    {
        // Базовые стили для нодов
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        nodeStyle.alignment = TextAnchor.MiddleCenter;
        nodeStyle.normal.textColor = Color.white;
        
        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
        selectedNodeStyle.alignment = TextAnchor.MiddleCenter;
        selectedNodeStyle.normal.textColor = Color.white;
        
        // Создаем тестовые ноды
        CreateNode(new Vector2(100, 100), "Red Node", Color.red);
        CreateNode(new Vector2(300, 200), "Green Node", Color.green);
        CreateNode(new Vector2(200, 300), "Blue Node", Color.blue);
    }
    
    private void OnGUI()
    {
        DrawToolbar();
        DrawEditingPanel();
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);
        
        // Применяем зум и смещение
        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(zoom, zoom), Vector2.zero);
        GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one) * GUI.matrix;
        
        DrawNodes();
        DrawConnections();
        
        ProcessNodeEvents(Event.current);
        ProcessEvents(Event.current);
        
        // Восстанавливаем матрицу
        GUI.matrix = oldMatrix;
        
        if (GUI.changed) Repaint();
    }
    
    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            if (GUILayout.Button("New Node", EditorStyles.toolbarButton))
            {
                CreateNode(new Vector2(100, 100), "New Node", Color.white);
            }
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                Debug.Log("Saving nodes...");
            }
            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                Debug.Log("Loading nodes...");
            }
            if (GUILayout.Button("Help", EditorStyles.toolbarButton))
            {
                Debug.Log("Help clicked");
            }
            
            GUILayout.FlexibleSpace();
            
            // Отображение текущего зума
            GUILayout.Label($"Zoom: {zoom:P0}", EditorStyles.toolbarButton);
        }
        GUILayout.EndHorizontal();
    }
    
    private void DrawEditingPanel()
    {
        if (selectedNodeForEditing != null)
        {
            GUILayout.BeginArea(new Rect(position.width - 250, 25, 250, position.height - 25), EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Node Properties", EditorStyles.boldLabel);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Name:");
                newNodeName = EditorGUILayout.TextField(newNodeName);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Color:");
                newNodeColor = EditorGUILayout.ColorField(newNodeColor);
                
                EditorGUILayout.Space();
                if (GUILayout.Button("Apply Changes"))
                {
                    selectedNodeForEditing.title = newNodeName;
                    selectedNodeForEditing.color = newNodeColor;
                    selectedNodeForEditing = null;
                    GUI.changed = true;
                }
                
                if (GUILayout.Button("Cancel"))
                {
                    selectedNodeForEditing = null;
                }
            }
            GUILayout.EndArea();
        }
    }
    
    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / (gridSpacing * zoom));
        int heightDivs = Mathf.CeilToInt(position.height / (gridSpacing * zoom));
        
        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
        
        offset += drag * 0.5f;
        Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);
        
        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset,
                            new Vector3(gridSpacing * i, position.height, 0) + newOffset);
        }
        
        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset,
                            new Vector3(position.width, gridSpacing * j, 0) + newOffset);
        }
        
        Handles.color = Color.white;
        Handles.EndGUI();
    }
    
    private void DrawNodes()
    {
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Draw();
            }
        }
    }
    
    private void DrawConnections()
    {
        if (connections != null)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                connections[i].Draw();
            }
        }
    }
    
    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;
        
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0) // Левая кнопка мыши
                {
                    ClearNodeSelection();
                }
                if (e.button == 1) // Правая кнопка мыши
                {
                    ProcessContextMenu(e.mousePosition);
                }
                break;
                
            case EventType.MouseDrag:
                if (e.button == 0) // Левая кнопка мыши - перемещение
                {
                    OnDrag(e.delta);
                }
                break;
                
            case EventType.ScrollWheel:
                OnScroll(e.delta.y);
                e.Use();
                break;
        }
    }
    
    private void ProcessNodeEvents(Event e)
    {
        if (nodes != null)
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
        
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Drag(delta);
            }
        }
        
        offset += delta;
        
        GUI.changed = true;
    }
    
    private void OnScroll(float delta)
    {
        float oldZoom = zoom;
        zoom += delta * 0.01f;
        zoom = Mathf.Clamp(zoom, 0.1f, 2.0f);
        
        // Корректируем offset для сохранения позиции под курсором
        Vector2 mousePosition = Event.current.mousePosition;
        offset += (mousePosition / oldZoom) * (oldZoom - zoom);
        
        GUI.changed = true;
    }
    
    private void OnClickAddNode(Vector2 mousePosition)
    {
        CreateNode(mousePosition / zoom - offset / zoom, "New Node", Color.white);
    }
    
    private void CreateNode(Vector2 position, string title, Color color)
    {
        Node node = new Node(position, 200, 50, nodeStyle, selectedNodeStyle, OnClickRemoveNode, OnClickEditNode);
        node.title = title;
        node.color = color;
        nodes.Add(node);
    }
    
    private void OnClickRemoveNode(Node node)
    {
        if (selectedNodeForEditing == node)
        {
            selectedNodeForEditing = null;
        }
        nodes.Remove(node);
    }
    
    private void OnClickEditNode(Node node)
    {
        selectedNodeForEditing = node;
        newNodeName = node.title;
        newNodeColor = node.color;
    }
    
    private void ClearNodeSelection()
    {
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].isSelected = false;
            }
        }
    }
}

public class Node
{
    public Rect rect;
    public string title;
    public Color color;
    public bool isDragged;
    public bool isSelected;
    
    private GUIStyle style;
    private GUIStyle selectedStyle;
    
    private System.Action<Node> OnRemoveNode;
    private System.Action<Node> OnEditNode;
    
    private Texture2D coloredBackground;
    
    public Node(Vector2 position, float width, float height, GUIStyle nodeStyle, GUIStyle selectedStyle, 
               System.Action<Node> OnClickRemoveNode, System.Action<Node> OnClickEditNode)
    {
        rect = new Rect(position.x, position.y, width, height);
        this.style = new GUIStyle(nodeStyle);
        this.selectedStyle = new GUIStyle(selectedStyle);
        title = "Node";
        color = Color.white;
        OnRemoveNode = OnClickRemoveNode;
        OnEditNode = OnClickEditNode;
        
        UpdateBackgroundColor();
    }
    
    public void UpdateBackgroundColor()
    {
        // Создаем текстуру с нужным цветом
        coloredBackground = new Texture2D(1, 1);
        coloredBackground.SetPixel(0, 0, color);
        coloredBackground.Apply();
        
        // Применяем к стилям
        style.normal.background = coloredBackground;
        selectedStyle.normal.background = coloredBackground;
    }
    
    public void Drag(Vector2 delta)
    {
        if (isDragged)
        {
            rect.position += delta;
        }
    }
    
    public void Draw()
    {
        // Обновляем цвет текста для контраста
        Color textColor = color.grayscale > 0.5f ? Color.black : Color.white;
        style.normal.textColor = textColor;
        selectedStyle.normal.textColor = textColor;
        
        GUI.Box(rect, title, isSelected ? selectedStyle : style);
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
                
                if (e.button == 1 && rect.Contains(e.mousePosition))
                {
                    ProcessContextMenu();
                    e.Use();
                }
                
                // Двойной клик для редактирования
                if (e.button == 0 && e.clickCount == 2 && rect.Contains(e.mousePosition))
                {
                    if (OnEditNode != null)
                    {
                        OnEditNode(this);
                        e.Use();
                    }
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
        genericMenu.AddItem(new GUIContent("Rename/Change Color"), false, () => OnEditNode?.Invoke(this));
        genericMenu.AddItem(new GUIContent("Remove node"), false, () => OnRemoveNode?.Invoke(this));
        genericMenu.ShowAsContext();
    }
}

public class Connection
{
    public void Draw()
    {
        // Реализация соединений между нодами
    }
}
}