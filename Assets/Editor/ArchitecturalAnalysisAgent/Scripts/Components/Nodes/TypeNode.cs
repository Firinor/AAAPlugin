using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FirUtility
{
    public class TypeNode : Node
    {
        public Type type;
        public MonoScript MonoScript;
        public TypeNode(Type type,
            NodeMapSettings mapSettings,
            Vector2 position,
            MonoScript monoScript = null,
            NodeMapSettings.NodeColor color = NodeMapSettings.NodeColor.Blue) 
            
            : base(type.ToString(), mapSettings, position, color)
        {
            this.type = type;
            MonoScript = monoScript;
            
            if (String.IsNullOrEmpty(name))
            {
                if (!String.IsNullOrEmpty(type.FullName)) name = type.FullName;
                else if (!String.IsNullOrEmpty(type.Name)) name = type.Name;
                else if (!String.IsNullOrEmpty(type.UnderlyingSystemType.ToString())) name = type.UnderlyingSystemType.ToString();
            }
        }
        
        protected override void ProcessContextMenu()
        {
            GenericMenu genericMenu = new GenericMenu();
            
            genericMenu.AddItem(new GUIContent("Open the information window"), false,
                () => Analyzer.ShowScriptInfo(type));
            genericMenu.AddItem(new GUIContent("Architectural analysis"), false, 
                () => map.OnAnalysisNodeByType?.Invoke(this));
            genericMenu.AddItem(new GUIContent("Add connection"), false, 
                () => map.OnAddConnection?.Invoke(this));
            genericMenu.AddItem(new GUIContent("Remove connections"), false, 
                () => map.OnRemoveConnections?.Invoke(this));
            genericMenu.AddItem(new GUIContent("Copy name"), false, 
                () => map.OnCopyNode?.Invoke(name.Split('.').Last()));
            genericMenu.AddItem(new GUIContent("Change node"), false, 
                () => map.OnEditNode?.Invoke(this));
            genericMenu.AddItem(new GUIContent("Remove node"), false, 
                () => OnRemoveNode?.Invoke(this));
            
            genericMenu.ShowAsContext();
        }
    }
}