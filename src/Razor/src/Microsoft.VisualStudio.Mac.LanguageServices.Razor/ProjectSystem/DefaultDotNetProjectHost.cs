// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using MonoDevelop.Projects;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    internal class DefaultDotNetProjectHost : DotNetProjectHost
    {
        private const string ExplicitRazorConfigurationCapability = "DotNetCoreRazorConfiguration";

        private readonly DotNetProject _project;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly VisualStudioMacWorkspaceAccessor _workspaceAccessor;
        private readonly TextBufferProjectService _projectService;
        private RazorProjectHostBase _razorProjectHost;

        public DefaultDotNetProjectHost(
            DotNetProject project,
            ForegroundDispatcher foregroundDispatcher,
            VisualStudioMacWorkspaceAccessor workspaceAccessor,
            TextBufferProjectService projectService)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (workspaceAccessor == null)
            {
                throw new ArgumentNullException(nameof(workspaceAccessor));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _project = project;
            _foregroundDispatcher = foregroundDispatcher;
            _workspaceAccessor = workspaceAccessor;
            _projectService = projectService;
        }

        // Internal for testing
        internal DefaultDotNetProjectHost(
            ForegroundDispatcher foregroundDispatcher,
            VisualStudioMacWorkspaceAccessor workspaceAccessor,
            TextBufferProjectService projectService)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (workspaceAccessor == null)
            {
                throw new ArgumentNullException(nameof(workspaceAccessor));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _workspaceAccessor = workspaceAccessor;
            _projectService = projectService;
        }

        public override DotNetProject Project => _project;

        public override void Subscribe()
        {
            _foregroundDispatcher.AssertForegroundThread();

            UpdateRazorHostProject();

            _project.ProjectCapabilitiesChanged += Project_ProjectCapabilitiesChanged;
            _project.Disposing += Project_Disposing;
        }

        private void Project_Disposing(object sender, EventArgs e)
        {
            _foregroundDispatcher.AssertForegroundThread();

            _project.ProjectCapabilitiesChanged -= Project_ProjectCapabilitiesChanged;
            _project.Disposing -= Project_Disposing;

            DetatchCurrentRazorProjectHost();
        }

        private void Project_ProjectCapabilitiesChanged(object sender, EventArgs e) => UpdateRazorHostProject();

        // Internal for testing
        internal void UpdateRazorHostProject()
        {
            _foregroundDispatcher.AssertForegroundThread();

            DetatchCurrentRazorProjectHost();

            if (!_projectService.IsSupportedProject(_project))
            {
                // Not a Razor compatible project.
                return;
            }

            if (!TryGetProjectSnapshotManager(out var projectSnapshotManager))
            {
                // Could not get a ProjectSnapshotManager for the current project.
                return;
            }

            if (_project.IsCapabilityMatch(ExplicitRazorConfigurationCapability))
            {
                // SDK >= 2.1
                _razorProjectHost = new DefaultRazorProjectHost(_project, _foregroundDispatcher, projectSnapshotManager);
                return;
            }

            // We're an older version of Razor at this point, SDK < 2.1
            _razorProjectHost = new FallbackRazorProjectHost(_project, _foregroundDispatcher, projectSnapshotManager);
        }

        private bool TryGetProjectSnapshotManager(out ProjectSnapshotManagerBase projectSnapshotManagerBase)
        {
            if (!_workspaceAccessor.TryGetWorkspace(_project.ParentSolution, out var workspace))
            {
                // Could not locate workspace for razor project. Project is most likely tearing down.
                projectSnapshotManagerBase = null;
                return false;
            }

            var languageService = workspace.Services.GetLanguageServices(RazorLanguage.Name);
            projectSnapshotManagerBase = (ProjectSnapshotManagerBase)languageService.GetRequiredService<ProjectSnapshotManager>();

            return true;
        }

        private void DetatchCurrentRazorProjectHost()
        {
            _razorProjectHost?.Detatch();
            _razorProjectHost = null;
        }
    }
}
