// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    internal class WorkspaceProjectSnapshotChangeTrigger : ProjectSnapshotChangeTrigger
    {
        private ProjectSnapshotManagerBase _projectManager;

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectManager = projectManager;
            _projectManager.Workspace.WorkspaceChanged += Workspace_WorkspaceChanged;

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
                        project = e.NewSolution.GetProject(e.ProjectId);
                        Debug.Assert(project != null);

                        _projectManager.WorkspaceProjectChanged(project);
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
    }
}
