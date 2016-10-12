// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.ProjectModel.Resolution;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.Extensions.ProjectModel
{
    internal class DotNetDependencyProvider
    {
        private ProjectContext _context;
        private List<DependencyDescription> _packageDependencies;
        private List<ResolvedReference> _resolvedReferences;
        private string _configuration;
        private List<string> _projectReferences;

        public DotNetDependencyProvider(ProjectContext context, string configuration = Constants.DefaultConfiguration)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _configuration = configuration;
            _context = context;
            DiscoverDependencies();
        }

        public IEnumerable<string> GetProjectReferences()
        {
            return _projectReferences;
        }

        public IEnumerable<DependencyDescription> GetPackageDependencies()
        {
            return _packageDependencies;
        }

        public IEnumerable<ResolvedReference> GetResolvedReferences()
        {
            return _resolvedReferences;
        }

        private void DiscoverDependencies()
        {
            var exporter = _context.CreateExporter(_configuration);

            if (exporter == null)
            {
                throw new InvalidOperationException($"Couldn't create a library exporter for configuration {_configuration}");
            }

            var framework = _context.TargetFramework;
            if (framework == null)
            {
                throw new InvalidOperationException("Couldn't resolve dependencies when target framework is not specified.");
            }

            var exports = exporter.GetDependencies();
            _resolvedReferences = new List<ResolvedReference>();
            _packageDependencies = new List<DependencyDescription>();
            _projectReferences = new List<string>();

            foreach (var export in exports)
            {
                var library = export.Library;
                var project = library as ProjectDescription;
                if (project != null)
                {
                    _projectReferences.Add(project.Project.ProjectFilePath);
                }
                else
                {
                    var description = new DependencyDescription(
                        library.Identity.Name,
                        library.Identity.Version.ToString(),
                        export.Library.Path,
                        framework.DotNetFrameworkName,
                        library.Identity.Type.Value,
                        library.Resolved);

                    foreach (var dependency in export.Library.Dependencies)
                    {
                        var dep = new Dependency(dependency.Name, version: string.Empty);
                        description.AddDependency(dep);
                    }


                    var itemSpec = $"{framework.DotNetFrameworkName}/{library.Identity.Name}/{library.Identity.Version.ToString()}";
                    _packageDependencies.Add(description);
                }

                // For resolved references we need to include all type of dependencies.
                if (library.Resolved)
                {
                    foreach (var asset in export.CompilationAssemblies)
                    {
                        var resolvedRef = new ResolvedReference(
                            name: Path.GetFileNameWithoutExtension(asset.FileName),
                            resolvedPath: asset.ResolvedPath);
                        _resolvedReferences.Add(resolvedRef);
                    }
                }
            }
        }
    }
}
