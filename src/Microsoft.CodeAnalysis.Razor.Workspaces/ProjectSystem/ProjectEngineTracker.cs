// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectEngineTracker
    {
        private const ProjectDifference Mask = ProjectDifference.ConfigurationChanged;

        private readonly object _lock = new object();

        private readonly HostWorkspaceServices _services;
        private RazorProjectEngine _projectEngine;

        public ProjectEngineTracker(ProjectState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _services = state.Services;
        }

        public ProjectEngineTracker ForkFor(ProjectState state, ProjectDifference difference)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if ((difference & Mask) != 0)
            {
                return null;
            }

            return this;
        }

        public RazorProjectEngine GetProjectEngine(ProjectState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (_projectEngine == null)
            {
                lock (_lock)
                {
                    if (_projectEngine == null)
                    {
                        var factory = _services.GetRequiredService<ProjectSnapshotProjectEngineFactory>();
                        _projectEngine = factory.Create(state.HostProject.Configuration, Path.GetDirectoryName(state.HostProject.FilePath), configure: null);
                    }
                }
            }

            return _projectEngine;
        }

        public List<string> GetImportDocumentTargetPaths(ProjectState state, string targetPath)
        {
            var projectEngine = GetProjectEngine(state);
            var importFeature = projectEngine.ProjectFeatures.OfType<IImportProjectFeature>().FirstOrDefault();
            var projectItem = projectEngine.FileSystem.GetItem(targetPath);
            var importItems = importFeature?.GetImports(projectItem).Where(i => i.FilePath != null);

            // Target path looks like `Foo\\Bar.cshtml`
            var targetPaths = new List<string>();
            foreach (var importItem in importItems)
            {
                var itemTargetPath = importItem.FilePath.Replace('/', '\\').TrimStart('\\');
                targetPaths.Add(itemTargetPath);
            }

            return targetPaths;
        }
    }
}
