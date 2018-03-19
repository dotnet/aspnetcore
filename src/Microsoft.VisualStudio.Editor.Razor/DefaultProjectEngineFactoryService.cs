// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultProjectEngineFactoryService : RazorProjectEngineFactoryService
    {
        private readonly static RazorConfiguration DefaultConfiguration = FallbackRazorConfiguration.MVC_2_1;

        private readonly ProjectSnapshotManager _projectManager;
        private readonly IFallbackProjectEngineFactory _defaultFactory;
        private readonly Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>[] _customFactories;

        public DefaultProjectEngineFactoryService(
           ProjectSnapshotManager projectManager,
           IFallbackProjectEngineFactory defaultFactory,
           Lazy<IProjectEngineFactory, ICustomProjectEngineFactoryMetadata>[] customFactories)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            if (defaultFactory == null)
            {
                throw new ArgumentNullException(nameof(defaultFactory));
            }

            if (customFactories == null)
            {
                throw new ArgumentNullException(nameof(customFactories));
            }

            _projectManager = projectManager;
            _defaultFactory = defaultFactory;
            _customFactories = customFactories;
        }

        public override IProjectEngineFactory FindFactory(ProjectSnapshot project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return SelectFactory(project.Configuration ?? DefaultConfiguration, requireSerializable: false);
        }

        public override IProjectEngineFactory FindSerializableFactory(ProjectSnapshot project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return SelectFactory(project.Configuration ?? DefaultConfiguration, requireSerializable: true);
        }

        public override RazorProjectEngine Create(ProjectSnapshot project, Action<RazorProjectEngineBuilder> configure)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return CreateCore(project, RazorProjectFileSystem.Create(Path.GetDirectoryName(project.FilePath)), configure);
        }

        public override RazorProjectEngine Create(string directoryPath, Action<RazorProjectEngineBuilder> configure)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            var project = FindProjectByDirectory(directoryPath);
            return CreateCore(project, RazorProjectFileSystem.Create(directoryPath), configure);
        }

        public override RazorProjectEngine Create(ProjectSnapshot project, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            return CreateCore(project, fileSystem, configure);
        }

        private RazorProjectEngine CreateCore(ProjectSnapshot project, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            // When we're running in the editor, the editor provides a configure delegate that will include
            // the editor settings and tag helpers.
            // 
            // This service is only used in process in Visual Studio, and any other callers should provide these
            // things also.
            configure = configure ?? ((b) => { });

            // The default configuration currently matches the newest MVC configuration.
            //
            // We typically want this because the language adds features over time - we don't want to a bunch of errors
            // to show up when a document is first opened, and then go away when the configuration loads, we'd prefer the opposite.
            var configuration = project?.Configuration ?? DefaultConfiguration;

            // If there's no factory to handle the configuration then fall back to a very basic configuration.
            //
            // This will stop a crash from happening in this case (misconfigured project), but will still make
            // it obvious to the user that something is wrong.
            var factory = SelectFactory(configuration) ?? _defaultFactory;
            return factory.Create(configuration, fileSystem, configure);
        }

        private IProjectEngineFactory SelectFactory(RazorConfiguration configuration, bool requireSerializable = false)
        {
            for (var i = 0; i < _customFactories.Length; i++)
            {
                var factory = _customFactories[i];
                if (string.Equals(configuration.ConfigurationName, factory.Metadata.ConfigurationName))
                {
                    return requireSerializable && !factory.Metadata.SupportsSerialization ? null : factory.Value;
                }
            }

            return null;
        }

        private ProjectSnapshot FindProjectByDirectory(string directory)
        {
            directory = NormalizeDirectoryPath(directory);

            var projects = _projectManager.Projects;
            for (var i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                if (project.FilePath != null)
                {
                    if (string.Equals(directory, NormalizeDirectoryPath(Path.GetDirectoryName(project.FilePath)), StringComparison.OrdinalIgnoreCase))
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