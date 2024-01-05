using System;
using System.Diagnostics;

namespace T3.Editor.Compilation;

public readonly struct DependencyInfo
{
    public readonly string Name;
    public readonly string Version;
    public readonly DependencyType DependencyType;

    public DependencyInfo(string name, DependencyType dependencyType)
    {
        Name = name;
        Version = string.Empty;

        Debug.Assert(dependencyType != DependencyType.PackageReference);
        DependencyType = dependencyType;
    }

    public DependencyInfo(string name, string version, DependencyType dependencyType)
    {
        Name = name;
        Version = version;

        Debug.Assert(dependencyType == DependencyType.PackageReference);
        DependencyType = dependencyType;
    }

    public DependencyInfo AdjustPath(string oldCsprojPath, string newCsprojPath)
    {
        throw new NotImplementedException();
    }
}

public enum DependencyType
{
    ProjectReference,
    PackageReference,
    DllReference
}