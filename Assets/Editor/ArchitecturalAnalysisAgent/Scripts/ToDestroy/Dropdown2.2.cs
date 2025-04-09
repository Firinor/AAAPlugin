using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// Пример использования в EditorWindow
public class DropdownExample2 : EditorWindow
{
    private string _selectedItemPath = "None";
    
    [MenuItem("FirUtils/Dropdown Example22")]
    static void ShowWindow()
    {
        GetWindow<DropdownExample2>("Dropdown Example");
    }

    void OnGUI()
    {
        // Отображаем текущий выбор
        EditorGUILayout.LabelField("Selected Item:", _selectedItemPath);
        
        if (GUILayout.Button("Show Dropdown"))
        {
            /*var dropdown = new NestedSearchDropdown(
                new AdvancedDropdownState(),
                (path) => { _selectedItemPath = path; Repaint(); });

            dropdown.Show(new Rect(0,0,0,20));//GUILayoutUtility.GetLastRect());*/
        }
    }
}