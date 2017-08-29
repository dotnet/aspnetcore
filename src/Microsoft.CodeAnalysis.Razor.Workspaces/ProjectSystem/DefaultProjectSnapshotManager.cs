// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DefaultProjectSnapshotManager : ProjectSnapshotManager
    {
        private readonly Workspace _workspace;
        private readonly Dictionary<ProjectId, ProjectSnapshot> _projects;
        private readonly List<WeakReference<DefaultProjectSnapshotListener>> _listeners;

        public DefaultProjectSnapshotManager(Workspace workspace)
        {
            _workspace = workspace;

            _projects = new Dictionary<ProjectId, ProjectSnapshot>();
            _listeners = new List<WeakReference<DefaultProjectSnapshotListener>>();

            // Attaching the event handler inside before initialization prevents re-entrancy without
            // losing any notifications.
            _workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
            InitializeSolution(_workspace.CurrentSolution);
        }

        public override IReadOnlyList<ProjectSnapshot> Projects => _projects.Values.ToArray();

        public override ProjectSnapshot FindProject(string projectPath)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            foreach (var project in _projects.Values)
            { 
                if (string.Equals(projectPath, project.UnderlyingProject.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    return project;
                }
            }

            return null;
        }

        public override ProjectSnapshotListener Subscribe()
        {
            var subscription = new DefaultProjectSnapshotListener();
            _listeners.Add(new WeakReference<DefaultProjectSnapshotListener>(subscription));

            return subscription;
        }

        private void InitializeSolution(Solution solution)
        {
            Debug.Assert(solution != null);

            foreach (var kvp in _projects.ToArray())
            {
                _projects.Remove(kvp.Key);
                NotifyListeners(new ProjectChangeEventArgs(kvp.Value, ProjectChangeKind.Removed));
            }

            foreach (var project in solution.Projects)
            {
                var projectState = new DefaultProjectSnapshot(project);
                _projects[project.Id] = projectState;

                NotifyListeners(new ProjectChangeEventArgs(projectState, ProjectChangeKind.Added));
            }
        }

        private void NotifyListeners(ProjectChangeEventArgs e)
        {
            for (var i = 0; i < _listeners.Count; i++)
            {
                if (_listeners[i].TryGetTarget(out var listener))
                {
                    listener.Notify(e);
                }
                else
                {
                    _listeners.RemoveAt(i--);
                }
            }
        }

        // Internal for testing
        internal void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            Project underlyingProject;
            ProjectSnapshot snapshot;
            switch (e.Kind)
            {
                case WorkspaceChangeKind.ProjectAdded:
                    {
                        underlyingProject = e.NewSolution.GetProject(e.ProjectId);
                        Debug.Assert(underlyingProject != null);

                        snapshot = new DefaultProjectSnapshot(underlyingProject);
                        _projects[e.ProjectId] = snapshot;

                        NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Added));
                        break;
                    }

                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                    {
                        underlyingProject = e.NewSolution.GetProject(e.ProjectId);
                        Debug.Assert(underlyingProject != null);

                        snapshot = new DefaultProjectSnapshot(underlyingProject);
                        _projects[e.ProjectId] = snapshot;

                        NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Changed));
                        break;
                    }

                case WorkspaceChangeKind.ProjectRemoved:
                    {
                        // We're being extra defensive here to avoid crashes.
                        if (_projects.TryGetValue(e.ProjectId, out snapshot))
                        {
                            _projects.Remove(e.ProjectId);
                            NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Removed));
                        }

                        break;
                    }

                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:
                    InitializeSolution(e.NewSolution);
                    break;
            }
        }
    }
}
