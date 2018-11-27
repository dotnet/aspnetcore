// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    internal class ProjectBuildChangeTrigger : ProjectSnapshotChangeTrigger
    {
        private readonly TextBufferProjectService _projectService;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private ProjectSnapshotManagerBase _projectManager;

        [ImportingConstructor]
        public ProjectBuildChangeTrigger(ForegroundDispatcher foregroundDispatcher, TextBufferProjectService projectService)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectService = projectService;
        }

        // Internal for testing
        internal ProjectBuildChangeTrigger(
            ForegroundDispatcher foregroundDispatcher,
            TextBufferProjectService projectService,
            ProjectSnapshotManagerBase projectManager)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectService = projectService;
            _projectManager = projectManager;
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;

            IdeApp.ProjectOperations.EndBuild += ProjectOperations_EndBuild;
        }

        // Internal for testing
        internal void ProjectOperations_EndBuild(object sender, BuildEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (!args.Success)
            {
                // Build failed
                return;
            }

            var projectItem = args.SolutionItem;
            if (!_projectService.IsSupportedProject(projectItem))
            {
                // We're hooked into all build events, it's possible to get called with an unsupported project item type.
                return;
            }

            var projectPath = _projectService.GetProjectPath(projectItem);

            // Get the corresponding roslyn project by matching the project name and the project path.
            foreach (var projectSnapshot in _projectManager.Projects)
            {
                if (string.Equals(projectPath, projectSnapshot.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    _projectManager.HostProjectBuildComplete(projectSnapshot.HostProject);
                    break;
                }
            }
        }
    }
}
