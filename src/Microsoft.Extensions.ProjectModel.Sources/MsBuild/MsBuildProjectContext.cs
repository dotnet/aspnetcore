// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Execution;
using NuGet.Frameworks;
using System.Linq;

namespace Microsoft.Extensions.ProjectModel
{
    internal class MsBuildProjectContext : IProjectContext
    {
        private const string CompileItemName = "Compile";
        private const string EmbedItemName = "EmbeddedResource";
        private const string FullPathMetadataName = "FullPath";

        private readonly ProjectInstance _project;
        private readonly string _name;

        public MsBuildProjectContext(string name, string configuration, ProjectInstance project)
        {
            _project = project;

            Configuration = configuration;
            _name = name;
        }

        public string FindProperty(string propertyName)
        {
            return _project.GetProperty(propertyName)?.EvaluatedValue;
        }

        public string ProjectName => FindProperty("ProjectName") ?? _name;
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
        public string AssemblyFullPath => FindProperty("TargetPath");
        public string Platform => FindProperty("Platform");
        public string ProjectFullPath => _project.FullPath;
        public string RootNamespace => FindProperty("RootNamespace") ?? ProjectName;
        public string TargetDirectory => FindProperty("TargetDir");

        public IEnumerable<string> CompilationItems
            => _project.GetItems(CompileItemName).Select(i => i.GetMetadataValue(FullPathMetadataName));

        public IEnumerable<string> EmbededItems
             => _project.GetItems(EmbedItemName).Select(i => i.GetMetadataValue(FullPathMetadataName));
    }
}