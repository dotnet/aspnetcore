// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.ProjectModel.Resolution;
using NuGet.Frameworks;

namespace Microsoft.Extensions.ProjectModel
{
    public interface IProjectContext
    {
        string ProjectName { get; }
        string Configuration { get; }
        string Platform { get; }
        string ProjectFullPath { get; }
        string RootNamespace { get; }
        bool IsClassLibrary { get; }
        NuGetFramework TargetFramework { get; }
        string Config { get; }
        string DepsJson { get; }
        string RuntimeConfigJson { get; }
        string PackageLockFile { get; }
        string PackagesDirectory { get; }
        string TargetDirectory { get; }
        string AssemblyFullPath { get; }
        IEnumerable<string> CompilationItems { get; }
        IEnumerable<string> EmbededItems { get; }
        string FindProperty(string propertyName);
        IEnumerable<DependencyDescription> PackageDependencies { get;}
        IEnumerable<ResolvedReference> CompilationAssemblies { get; }
    }
}