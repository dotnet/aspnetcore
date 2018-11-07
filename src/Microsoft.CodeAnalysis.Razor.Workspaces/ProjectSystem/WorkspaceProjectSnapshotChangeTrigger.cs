// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    internal class WorkspaceProjectSnapshotChangeTrigger : ProjectSnapshotChangeTrigger
    {
        private ProjectSnapshotManagerBase _projectManager;

        public int ProjectChangeDelay { get;  set; } = 3 * 1000;

        // We throttle updates to projects to prevent doing too much work while the projects
        // are being initialized.
        //
        // Internal for testing
        internal Dictionary<ProjectId, Task> _deferredUpdates;

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectManager = projectManager;
            _projectManager.Workspace.WorkspaceChanged += Workspace_WorkspaceChanged;

            _deferredUpdates = new Dictionary<ProjectId, Task>();

            InitializeSolution(_projectManager.Workspace.CurrentSolution);
        }

        private void InitializeSolution(Solution solution)
        {
            Debug.Assert(solution != null);

            foreach (var project in solution.Projects)
            {
                _projectManager.WorkspaceProjectAdded(project);
            }
        }

        // Internal for testing
        internal void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            Project project;
            switch (e.Kind)
            {
                case WorkspaceChangeKind.ProjectAdded:
                    {
                        project = e.NewSolution.GetProject(e.ProjectId);
                        Debug.Assert(project != null);

                        _projectManager.WorkspaceProjectAdded(project);
                        break;
                    }

                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                    {
                        EnqueueUpdate(e.ProjectId);
                        break;
                    }

                case WorkspaceChangeKind.ProjectRemoved:
                    {
                        project = e.OldSolution.GetProject(e.ProjectId);
                        Debug.Assert(project != null);

                        _projectManager.WorkspaceProjectRemoved(project);
                        break;
                    }

                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:

                    if (e.OldSolution != null)
                    {
                        foreach (var p in e.OldSolution.Projects)
                        {
                            _projectManager.WorkspaceProjectRemoved(p);
                        }
                    }

                    InitializeSolution(e.NewSolution);
                    break;
            }
        }

        private void EnqueueUpdate(ProjectId projectId)
        {
            // A race is not possible here because we use the main thread to synchronize the updates
            // by capturing the sync context.
            if (!_deferredUpdates.TryGetValue(projectId, out var update) || update.IsCompleted)
            {
                _deferredUpdates[projectId] = UpdateAfterDelay(projectId);
            }
        }

        private async Task UpdateAfterDelay(ProjectId projectId)
        {
            await Task.Delay(ProjectChangeDelay);

            var solution = _projectManager.Workspace.CurrentSolution;
            var workspaceProject = solution.GetProject(projectId);
            if (workspaceProject != null)
            {
                _projectManager.WorkspaceProjectChanged(workspaceProject);
            }
        }
    }
}
