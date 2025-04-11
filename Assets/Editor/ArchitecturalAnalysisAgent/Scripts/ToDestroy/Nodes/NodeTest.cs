using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NodeTest
{
    public class NodeEditorWindow : EditorWindow
    {
        private List<Node> nodes = new List<Node>();
        private List<Connection> connections = new List<Connection>();

        private GUIStyle nodeStyle;
        private GUIStyle selectedNodeStyle;

        private Vector2 offset;
        private Vector2 drag;
        private float zoom = 1.0f;

        [MenuItem("FirUtils/Node Editor222")]
        private static void OpenWindow()
        {
            NodeEditorWindow window = GetWindow<NodeEditorWindow>();
            window.titleContent = new GUIContent("Node Test Editor");
        }

        private void OnEnable()
        {
            // Стили для нодов
            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
            nodeStyle.alignment = TextAnchor.MiddleCenter;
            nodeStyle.normal.textColor = Color.white;

            selectedNodeStyle = new GUIStyle();
            selectedNodeStyle.normal.background =
                EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
            selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
            selectedNodeStyle.alignment = TextAnchor.MiddleCenter;
            selectedNodeStyle.normal.textColor = Color.white;

            // Создаем тестовые ноды
            CreateNode(new Vector2(100, 100));
            CreateNode(new Vector2(300, 200));
        }

        private void OnGUI()
        {
            DrawToolbar();
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
                if (GUILayout.Button("Button 1", EditorStyles.toolbarButton))
                {
                    Debug.Log("Button 1 clicked");
                }

                if (GUILayout.Button("Button 2", EditorStyles.toolbarButton))
                {
                    Debug.Log("Button 2 clicked");
                }

                if (GUILayout.Button("Button 3", EditorStyles.toolbarButton))
                {
                    Debug.Log("Button 3 clicked");
                }

                if (GUILayout.Button("Button 4", EditorStyles.toolbarButton))
                {
                    Debug.Log("Button 4 clicked");
                }

                GUILayout.FlexibleSpace();

                // Отображение текущего зума
                GUILayout.Label($"Zoom: {zoom:P0}", EditorStyles.toolbarButton);
            }
            GUILayout.EndHorizontal();
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
                    OnScroll(-e.delta.y);
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
            CreateNode(mousePosition);
        }

        private void CreateNode(Vector2 position)
        {
            Node node = new Node(position, 200, 50, nodeStyle, selectedNodeStyle, OnClickRemoveNode);
            nodes.Add(node);
        }

        private void OnClickRemoveNode(Node node)
        {
            nodes.Remove(node);
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
        public bool isDragged;
        public bool isSelected;

        private GUIStyle style;
        private GUIStyle selectedStyle;

        private System.Action<Node> OnRemoveNode;

        public Node(Vector2 position, float width, float height, GUIStyle nodeStyle, GUIStyle selectedNodeStyle,
            System.Action<Node> OnClickRemoveNode)
        {
            rect = new Rect(position.x, position.y, width, height);
            style = nodeStyle;
            selectedStyle = selectedNodeStyle;
            title = "Node " + (position.x + position.y).ToString();
            OnRemoveNode = OnClickRemoveNode;
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

    public class Connection
    {
        public void Draw()
        {
            // Реализация соединений между нодами
        }
    }
}