using System;
using System.Reflection;
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
        
        public int xStep = 50;
        public int yStep = 50;
        public int gap = 40;

        //Event bus
        public Action<TypeNode> OnAnalysisNodeByType;
        public Action<AssemblyNode> OnAnalysisNodeByAssembly;
        
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