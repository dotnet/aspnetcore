// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Execution;
using NuGet.Frameworks;
using System.Linq;
using Microsoft.Extensions.ProjectModel.Resolution;

namespace Microsoft.Extensions.ProjectModel
{
    internal class MsBuildProjectContext : IProjectContext
    {
        private const string CompileItemName = "Compile";
        private const string EmbedItemName = "EmbeddedResource";
        private const string FullPathMetadataName = "FullPath";

        private readonly MsBuildProjectDependencyProvider _dependencyProvider;
        private IEnumerable<DependencyDescription> _packageDependencies;
        private IEnumerable<ResolvedReference> _compilationAssemblies;

        protected ProjectInstance Project { get; }
        protected string Name { get; }

        public MsBuildProjectContext(string name, string configuration, ProjectInstance project)
        {
            Project = project;

            Configuration = configuration;
            Name = name;
            _dependencyProvider = new MsBuildProjectDependencyProvider(Project);
        }

        public string FindProperty(string propertyName)
        {
            return Project.GetProperty(propertyName)?.EvaluatedValue;
        }

        public string ProjectName => FindProperty("ProjectName") ?? Name;
        public string Configuration { get; }

        public NuGetFramework TargetFramework
        {
            get
            {
                var tfm = FindProperty("NuGetTargetMoniker") ?? FindProperty("TargetFramework");
                if (tfm == null)
                {
                    return null;
                }
                return NuGetFramework.Parse(tfm);
            }
        }

        public bool IsClassLibrary => FindProperty("OutputType").Equals("Library", StringComparison.OrdinalIgnoreCase);

        // TODO get from actual properties according to TFM
        public string Config => AssemblyFullPath + ".config";
        public string DepsJson => Path.Combine(TargetDirectory, Path.GetFileNameWithoutExtension(AssemblyFullPath) + ".deps.json");
        public string RuntimeConfigJson => Path.Combine(TargetDirectory, Path.GetFileNameWithoutExtension(AssemblyFullPath) + ".runtimeconfig.json");
        public string PackagesDirectory => FindProperty("NuGetPackageRoot");
        public string PackageLockFile
        {
            get
            {
                var restoreOutputPath = FindProperty("RestoreOutputPath");
                if (string.IsNullOrEmpty(restoreOutputPath))
                {
                    restoreOutputPath = Path.Combine(Path.GetDirectoryName(ProjectFullPath), "obj");
                }
                return Path.Combine(restoreOutputPath, "project.assets.json");
            }
        }
        public string AssemblyFullPath => FindProperty("TargetPath");
        public string Platform => FindProperty("Platform");
        public string ProjectFullPath => Project.FullPath;
        public string RootNamespace => FindProperty("RootNamespace") ?? ProjectName;
        public string TargetDirectory => FindProperty("TargetDir");

        public IEnumerable<string> CompilationItems
            => Project.GetItems(CompileItemName).Select(i => i.GetMetadataValue(FullPathMetadataName));

        public IEnumerable<string> EmbededItems
             => Project.GetItems(EmbedItemName).Select(i => i.GetMetadataValue(FullPathMetadataName));

        public IEnumerable<DependencyDescription> PackageDependencies
        {
            get
            {
                if (_packageDependencies == null)
                {
                    _packageDependencies = _dependencyProvider.GetPackageDependencies();
                }

                return _packageDependencies;
            }
        }

        public IEnumerable<ResolvedReference> CompilationAssemblies
        {
            get
            {
                if (_compilationAssemblies == null)
                {
                    _compilationAssemblies = _dependencyProvider.GetResolvedReferences();
                }

                return _compilationAssemblies;
            }
        }

    }
}