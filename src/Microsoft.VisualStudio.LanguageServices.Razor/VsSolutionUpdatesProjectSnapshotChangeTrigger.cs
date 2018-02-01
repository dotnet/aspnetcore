// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    internal class VsSolutionUpdatesProjectSnapshotChangeTrigger : ProjectSnapshotChangeTrigger, IVsUpdateSolutionEvents2
    {
        private readonly IServiceProvider _services;
        private readonly TextBufferProjectService _projectService;

        private ProjectSnapshotManagerBase _projectManager;

        [ImportingConstructor]
        public VsSolutionUpdatesProjectSnapshotChangeTrigger(
            [Import(typeof(SVsServiceProvider))] IServiceProvider services,
            TextBufferProjectService projectService)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (projectService == null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _services = services;
            _projectService = projectService;
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectManager = projectManager;

            // Attach the event sink to solution update events.
            var solutionBuildManager = _services.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            if (solutionBuildManager != null)
            {
                // We expect this to be called only once. So we don't need to Unadvise.
                var hr = solutionBuildManager.AdviseUpdateSolutionEvents(this, out var cookie);
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        // This gets called when the project has finished building.
        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            var projectName = _projectService.GetProjectName(pHierProj);
            var projectPath = _projectService.GetProjectPath(pHierProj);

            // Get the corresponding roslyn project by matching the project name and the project path.
            foreach (var project in _projectManager.Workspace.CurrentSolution.Projects)
            {
                if (string.Equals(projectName, project.Name, StringComparison.Ordinal) &&
                    string.Equals(projectPath, project.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    _projectManager.ProjectBuildComplete(project);
                    break;
                }
            }

            return VSConstants.S_OK;
        }
    }
}
