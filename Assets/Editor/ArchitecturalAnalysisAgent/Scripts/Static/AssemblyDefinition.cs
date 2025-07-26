using System;
using System.Reflection;
using UnityEditorInternal;

namespace FirUtility
{
    [Serializable]
    public class AssemblyDefinition
    {
        public string name;
        public string[] references;
    }

    [Serializable]
    public record AssemblyBindData
    {
        public Assembly Assembly;
        public AssemblyDefinitionAsset AssemblyDefinitionAsset;
    }
}