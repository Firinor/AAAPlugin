using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SearchableNestedDropdownTest
{
    private struct MenuItem
    {
        public string Path;
        public string Name;
        public GenericMenu.MenuFunction Function;
    }

    private List<MenuItem> items = new List<MenuItem>();
    private string searchString = "";

    public void AddItem(string path, GenericMenu.MenuFunction function)
    {
        var split = path.Split('/');
        items.Add(new MenuItem
        {
            Path = path,
            Name = split[split.Length - 1],
            Function = function
        });
    }

    public void Show(Rect position)
    {
        var window = EditorWindow.GetWindow<SearchableNestedDropdownWindow>();
        window.position = new Rect(position.x, position.y + position.height, 300, 400);
        window.Init(this);
        window.ShowPopup();
    }

    private class SearchableNestedDropdownWindow : EditorWindow
    {
        private SearchableNestedDropdownTest dropdown;
        private Vector2 scrollPos;

        public void Init(SearchableNestedDropdownTest dropdown)
        {
            this.dropdown = dropdown;
        }

        void OnGUI()
        {
            // Поле поиска
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            dropdown.searchString = GUILayout.TextField(dropdown.searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
            {
                dropdown.searchString = "";
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();

            // Отфильтрованные элементы
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var item in dropdown.items)
            {
                if (string.IsNullOrEmpty(dropdown.searchString) || 
                    item.Name.ToLower().Contains(dropdown.searchString.ToLower()) || 
                    item.Path.ToLower().Contains(dropdown.searchString.ToLower()))
                {
                    if (GUILayout.Button(item.Path, EditorStyles.miniButton))
                    {
                        item.Function?.Invoke();
                        Close();
                    }
                }
            }
            GUILayout.EndScrollView();

            // Закрытие при клике вне окна
            if (Event.current.type == EventType.MouseDown && !position.Contains(Event.current.mousePosition))
            {
                Close();
            }
        }
    }
}

// Пример использования:
public class TestWindow : EditorWindow
{
    [MenuItem("FirUtils/Test Dropdown3")]
    static void ShowWindow()
    {
        GetWindow<TestWindow>("Test Dropdown");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Show Nested Dropdown"))
        {
            var dropdown = new SearchableNestedDropdownTest();
            dropdown.AddItem("Category 1/Item 1", () => Debug.Log("Item 1"));
            dropdown.AddItem("Category 1/Item 2", () => Debug.Log("Item 2"));
            dropdown.AddItem("Category 2/Subcategory/Item 3", () => Debug.Log("Item 3"));
            dropdown.AddItem("Category 2/Item 4", () => Debug.Log("Item 4"));
            dropdown.Show(new Rect(0, 0, 200, 20));
        }
    }
}