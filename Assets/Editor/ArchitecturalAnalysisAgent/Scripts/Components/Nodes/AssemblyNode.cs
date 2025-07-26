using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FirUtility
{
    public class AssemblyNode : Node
    {
        public Assembly Assembly;
        public AssemblyDefinitionAsset AssemblyDefinitionAsset;
        
        public AssemblyNode(AssemblyBindData assemblyData,
            NodeMapSettings mapSettings,
            Vector2 position,
            NodeMapSettings.NodeColor color = NodeMapSettings.NodeColor.Grey) 
            
            : base(assemblyData.Assembly.GetName().Name, mapSettings, position, color)
        {
            Assembly = assemblyData.Assembly;
            AssemblyDefinitionAsset = assemblyData.AssemblyDefinitionAsset;
            
            if(AssemblyDefinitionAsset is not null 
                && Assembly.GetName().Name != AssemblyDefinitionAsset.name)
                name = Assembly.GetName().Name + " (" + AssemblyDefinitionAsset.name + ")";
        }
        
        protected override void ProcessContextMenu()
        {
            GenericMenu genericMenu = new GenericMenu();
            
            genericMenu.AddItem(new GUIContent("Open the information window"), false,
                () => Analyzer.ShowAssemblyInfo(Assembly.GetName().Name));
            genericMenu.AddItem(new GUIContent("Architectural analysis"), false, 
                () => map.OnAnalysisNodeByAssembly?.Invoke(this));
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