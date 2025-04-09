using UnityEditor;
using UnityEngine;

public class ExampleEditorWindow : EditorWindow
{
    [MenuItem("FirUtils/ToolTip Window2")]
    public static void ShowWindow()
    {
        GetWindow<ExampleEditorWindow>("ToolTip");
    }

    private void OnGUI()
    {
        // Создаём кнопку с подсказкой
        GUIContent buttonContent = new GUIContent("Кнопка", "Это подсказка при наведении");
        if (GUILayout.Button(buttonContent))
        {
            Debug.Log("Кнопка нажата!");
        }

        // Отображаем подсказку, если она есть
        if (!string.IsNullOrEmpty(GUI.tooltip))
        {
            // Получаем текущую позицию мыши
            Vector2 mousePosition = Event.current.mousePosition;
            // Стиль для подсказки (опционально)
            GUIStyle tooltipStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f)) },
                padding = new RectOffset(6, 6, 4, 4),
                fontSize = 10
            };

            // Рисуем подсказку
            Rect tooltipRect = new Rect(mousePosition.x + 15, mousePosition.y + 15, 200, 40);
            EditorGUI.LabelField(tooltipRect, GUI.tooltip, tooltipStyle);
            Repaint(); // Обновляем окно, чтобы подсказка не пропадала
        }
    }

    // Вспомогательная функция для создания текстуры
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}