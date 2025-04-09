using System;
using System.IO;
using System.Linq;
using System.Reflection;

public class AssemblyAnalyzer
{
    public static void Analyze(Assembly assembly)
    {
        // 1. Дата последнего изменения
        string assemblyPath = assembly.Location;
        DateTime lastModified = File.GetLastWriteTime(assemblyPath);
        
        var namespacesCount = assembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(n => n != null)
            .Distinct()
            .Count();
        
        var types = assembly.GetTypes();
        
        int classCount = types.Count(t => t.IsClass);
        int structCount = types.Count(t => t.IsValueType && !t.IsEnum);
        int enumCount = types.Count(t => t.IsEnum);
        int interfaceCount = types.Count(t => t.IsInterface);
    }
}