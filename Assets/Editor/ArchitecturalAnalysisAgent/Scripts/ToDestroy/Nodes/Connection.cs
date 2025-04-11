using UnityEditor;
using UnityEngine;

namespace TestToDestroy
{
    public class Connection
    {
        public ConnectionPoint inPoint;
        public ConnectionPoint outPoint;
        public System.Action<Connection> OnClickRemoveConnection;

        public Connection(ConnectionPoint inPoint, ConnectionPoint outPoint,
            System.Action<Connection> OnClickRemoveConnection)
        {
            this.inPoint = inPoint;
            this.outPoint = outPoint;
            this.OnClickRemoveConnection = OnClickRemoveConnection;
        }

        public void Draw()
        {
            Handles.DrawBezier(
                inPoint.rect.center,
                outPoint.rect.center,
                inPoint.rect.center + Vector2.left * 50f,
                outPoint.rect.center - Vector2.left * 50f,
                Color.white,
                null,
                2f
            );
            // Рассчитываем направление и позицию для стрелочки
            Vector2 direction = (inPoint.rect.center - outPoint.rect.center).normalized;
            float arrowSize = 10f;
            Vector2 arrowTip = inPoint.rect.center; // Отступаем немного от конечной точки

            // Рисуем стрелочку (треугольник)
            Vector2 arrowLeft = arrowTip - (Vector2)(Quaternion.Euler(0, 0, 30) * Vector3.right * arrowSize);
            Vector2 arrowRight = arrowTip - (Vector2)(Quaternion.Euler(0, 0, -30) * Vector3.right * arrowSize);

            Handles.DrawAAConvexPolygon(arrowTip, arrowLeft, arrowRight, arrowTip);

            // Кнопка для удаления соединения (оставляем в середине)
            if (Handles.Button((inPoint.rect.center + outPoint.rect.center) * 0.5f, Quaternion.identity, 4, 8,
                    Handles.RectangleHandleCap))
            {
                if (OnClickRemoveConnection != null)
                {
                    OnClickRemoveConnection(this);
                }
            }
        }
    }
}