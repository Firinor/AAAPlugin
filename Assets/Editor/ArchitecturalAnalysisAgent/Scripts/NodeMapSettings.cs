using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class NodeMapSettings
    {
        public NodeMapSettings(EditorWindow window)
        {
            this.window = window;
            Offset = new Vector2(this.window.position.width/2f, window.position.height/2f);
        }
        
        public float Zoom = 1;
        public Vector2 Offset;
        private EditorWindow window;
    }
}