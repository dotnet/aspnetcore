// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Mvc1_X = Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;
using MvcLatest = Microsoft.AspNetCore.Mvc.Razor.Extensions;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultProjectEngineFactoryService : RazorProjectEngineFactoryService
    {
        private readonly static MvcExtensibilityConfiguration DefaultConfiguration = new MvcExtensibilityConfiguration(
            RazorLanguageVersion.Version_2_0,
            ProjectExtensibilityConfigurationKind.Fallback,
            new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Razor.Language", new Version("2.0.0.0"))),
            new ProjectExtensibilityAssembly(new AssemblyIdentity("Microsoft.AspNetCore.Mvc.Razor", new Version("2.0.0.0"))));

        private readonly ProjectSnapshotManager _projectManager;

        public DefaultProjectEngineFactoryService(ProjectSnapshotManager projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
        }

        public override RazorProjectEngine Create(string projectPath, Action<RazorProjectEngineBuilder> configure)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            // In 15.5 we expect projectPath to be a directory, NOT the path to the csproj.
            var project = FindProject(projectPath);
            var configuration = (project?.Configuration as MvcExtensibilityConfiguration) ?? DefaultConfiguration;
            var razorLanguageVersion = configuration.LanguageVersion;

            var razorConfiguration = new RazorConfiguration(razorLanguageVersion, "unnamed", Array.Empty<RazorExtension>());
            var fileSystem = RazorProjectFileSystem.Create(projectPath);

            RazorProjectEngine projectEngine;
            if (razorLanguageVersion.Major == 1)
            {
                projectEngine = RazorProjectEngine.Create(razorConfiguration, fileSystem, b =>
                {
                    configure?.Invoke(b);

                    Mvc1_X.RazorExtensions.Register(b);

                    if (configuration.MvcAssembly.Identity.Version.Minor >= 1)
                    {
                        Mvc1_X.RazorExtensions.RegisterViewComponentTagHelpers(b);
                    }
                });
            }
            else
            {
                projectEngine = RazorProjectEngine.Create(razorConfiguration, fileSystem, b =>
                {
                    configure?.Invoke(b);

                    MvcLatest.RazorExtensions.Register(b);
                });
            }

            return projectEngine;
        }

        private ProjectSnapshot FindProject(string directory)
        {
            directory = NormalizeDirectoryPath(directory);

            var projects = _projectManager.Projects;
            for (var i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                if (project.UnderlyingProject.FilePath != null)
                {
                    if (string.Equals(directory, NormalizeDirectoryPath(Path.GetDirectoryName(project.UnderlyingProject.FilePath)), StringComparison.OrdinalIgnoreCase))
                    {
                        return project;
                    }
                }
            }

            return null;
        }

        private string NormalizeDirectoryPath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }
    }
}
