using System;
using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class NodeMapSettings
    {
        public Vector2 DefaultOffset => new Vector2(window.position.width/2f, window.position.height/2f);

        public NodeMapSettings(EditorWindow window)
        {
            this.window = window;
            Offset = DefaultOffset;
        }
        
        public float Zoom = 1;
        public Vector2 Offset;
        private EditorWindow window;

        
        //Event bus
        public Action<Type> OnAnalysisNode;
        public Action<Node> OnEditNode;
        public Action<string> OnCopyNode;
        public Action<Node> OnAddConnection;
        public Action<Node> OnRemoveConnections;
        public Action<Node> OnRemoveNode;

        public enum NodeColor
        {
            Grey = 0,
            Blue,
            Teal,
            Green,
            Yellow,
            Orange,
            Red
        }
    }
}