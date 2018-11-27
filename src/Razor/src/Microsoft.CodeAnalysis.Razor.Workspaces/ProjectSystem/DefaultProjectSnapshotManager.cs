// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // The implementation of project snapshot manager abstracts over the Roslyn Project (WorkspaceProject)
    // and information from the host's underlying project system (HostProject), to provide a unified and
    // immutable view of the underlying project systems.
    //
    // The HostProject support all of the configuration that the Razor SDK exposes via the project system
    // (language version, extensions, named configuration).
    //
    // The WorkspaceProject is needed to support our use of Roslyn Compilations for Tag Helpers and other
    // C# based constructs.
    //
    // The implementation will create a ProjectSnapshot for each HostProject. Put another way, when we
    // see a WorkspaceProject get created, we only care if we already have a HostProject for the same
    // filepath.
    //
    // Our underlying HostProject infrastructure currently does not handle multiple TFMs (project with
    // $(TargetFrameworks), so we just bind to the first WorkspaceProject we see for each HostProject.
    internal class DefaultProjectSnapshotManager : ProjectSnapshotManagerBase
    {
        public override event EventHandler<ProjectChangeEventArgs> Changed;

        private readonly ErrorReporter _errorReporter;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectSnapshotChangeTrigger[] _triggers;
        private readonly ProjectSnapshotWorkerQueue _workerQueue;
        private readonly ProjectSnapshotWorker _worker;

        private readonly Dictionary<string, DefaultProjectSnapshot> _projects;

        public DefaultProjectSnapshotManager(
            ForegroundDispatcher foregroundDispatcher,
            ErrorReporter errorReporter,
            ProjectSnapshotWorker worker,
            IEnumerable<ProjectSnapshotChangeTrigger> triggers,
            Workspace workspace)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (worker == null)
            {
                throw new ArgumentNullException(nameof(worker));
            }

            if (triggers == null)
            {
                throw new ArgumentNullException(nameof(triggers));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _errorReporter = errorReporter;
            _worker = worker;
            _triggers = triggers.ToArray();
            Workspace = workspace;

            _projects = new Dictionary<string, DefaultProjectSnapshot>(FilePathComparer.Instance);

            _workerQueue = new ProjectSnapshotWorkerQueue(_foregroundDispatcher, this, worker);

            for (var i = 0; i < _triggers.Length; i++)
            {
                _triggers[i].Initialize(this);
            }
        }

        public override IReadOnlyList<ProjectSnapshot> Projects
        {
            get
            {
                _foregroundDispatcher.AssertForegroundThread();
                return _projects.Values.ToArray();
            }
        }

        public override Workspace Workspace { get; }

        public override void ProjectUpdated(ProjectSnapshotUpdateContext update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (_projects.TryGetValue(update.WorkspaceProject.FilePath, out var original))
            {
                if (!original.IsInitialized)
                {
                    // If the project has been uninitialized, just ignore the update. 
                    return;
                }

                // This is an update to the project's computed values, so everything should be overwritten
                var snapshot = original.WithComputedUpdate(update);
                _projects[update.WorkspaceProject.FilePath] = snapshot;

                if (snapshot.IsDirty)
                {
                    // It's possible that the snapshot can still be dirty if we got a project update while computing state in
                    // the background. We need to trigger the background work to asynchronously compute the effect of the updates.
                    NotifyBackgroundWorker(snapshot.CreateUpdateContext());
                }
                
                if (!object.Equals(snapshot.ComputedVersion, original.ComputedVersion))
                {
                    NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.TagHelpersChanged));
                }
            }
        }

        public override void HostProjectAdded(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            _foregroundDispatcher.AssertForegroundThread();

            // We don't expect to see a HostProject initialized multiple times for the same path. Just ignore it.
            if (_projects.ContainsKey(hostProject.FilePath))
            {
                return;
            }

            // It's possible that Workspace has already created a project for this, but it's not deterministic
            // So if possible find a WorkspaceProject.
            var workspaceProject = GetWorkspaceProject(hostProject.FilePath);

            var snapshot = new DefaultProjectSnapshot(hostProject, workspaceProject);
            _projects[hostProject.FilePath] = snapshot;

            if (snapshot.IsInitialized && snapshot.IsDirty)
            {
                // Start computing background state if the project is fully initialized.
                NotifyBackgroundWorker(snapshot.CreateUpdateContext());
            }

            // We need to notify listeners about every project add.
            NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Added));
        }

        public override void HostProjectChanged(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (_projects.TryGetValue(hostProject.FilePath, out var original))
            {
                // Doing an update to the project should keep computed values, but mark the project as dirty if the
                // underlying project is newer.
                var snapshot = original.WithHostProject(hostProject);
                _projects[hostProject.FilePath] = snapshot;

                if (snapshot.IsInitialized && snapshot.IsDirty)
                {
                    // Start computing background state if the project is fully initialized.
                    NotifyBackgroundWorker(snapshot.CreateUpdateContext());
                }

                // Notify listeners right away because if the HostProject changes then it's likely that the Razor
                // configuration changed.
                NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Changed));
            }
        }

        public override void HostProjectRemoved(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (_projects.TryGetValue(hostProject.FilePath, out var snapshot))
            {
                _projects.Remove(hostProject.FilePath);

                // We need to notify listeners about every project removal.
                NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Removed));
            }
        }

        public override void HostProjectBuildComplete(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (_projects.TryGetValue(hostProject.FilePath, out var original))
            {
                var workspaceProject = GetWorkspaceProject(hostProject.FilePath);
                if (workspaceProject == null)
                {
                    // Host project was built prior to a workspace project being associated. We have nothing to do without
                    // a workspace project so we short circuit.
                    return;
                }

                // Doing an update to the project should keep computed values, but mark the project as dirty if the
                // underlying project is newer.
                var snapshot = original.WithWorkspaceProject(workspaceProject);

                _projects[hostProject.FilePath] = snapshot;

                // Notify the background worker so it can trigger tag helper discovery.
                NotifyBackgroundWorker(snapshot.CreateUpdateContext());
            }
        }

        public override void WorkspaceProjectAdded(Project workspaceProject)
        {
            if (workspaceProject == null)
            {
                throw new ArgumentNullException(nameof(workspaceProject));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (!IsSupportedWorkspaceProject(workspaceProject))
            {
                return;
            }

            // The WorkspaceProject initialization never triggers a "Project Add" from out point of view, we
            // only care if the new WorkspaceProject matches an existing HostProject.
            if (_projects.TryGetValue(workspaceProject.FilePath, out var original))
            {
                // If this is a multi-targeting project then we are only interested in a single workspace project. If we already
                // found one in the past just ignore this one.
                if (original.WorkspaceProject == null)
                {
                    var snapshot = original.WithWorkspaceProject(workspaceProject);
                    _projects[workspaceProject.FilePath] = snapshot;

                    if (snapshot.IsInitialized && snapshot.IsDirty)
                    {
                        // We don't need to notify listeners yet because we don't have any **new** computed state. 
                        //
                        // However we do need to trigger the background work to asynchronously compute the effect of the updates.
                        NotifyBackgroundWorker(snapshot.CreateUpdateContext());
                    }

                    // Notify listeners right away since WorkspaceProject was just added, the project is now initialized.
                    NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Changed));
                }
            }
        }

        public override void WorkspaceProjectChanged(Project workspaceProject)
        {
            if (workspaceProject == null)
            {
                throw new ArgumentNullException(nameof(workspaceProject));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (!IsSupportedWorkspaceProject(workspaceProject))
            {
                return;
            }

            // We also need to check the projectId here. If this is a multi-targeting project then we are only interested
            // in a single workspace project. Just use the one that showed up first.
            if (_projects.TryGetValue(workspaceProject.FilePath, out var original) &&
                (original.WorkspaceProject == null ||
                original.WorkspaceProject.Id == workspaceProject.Id))
            {
                // Doing an update to the project should keep computed values, but mark the project as dirty if the
                // underlying project is newer.
                var snapshot = original.WithWorkspaceProject(workspaceProject);
                _projects[workspaceProject.FilePath] = snapshot;

                if (snapshot.IsInitialized && snapshot.IsDirty)
                {
                    // We don't need to notify listeners yet because we don't have any **new** computed state. However we do 
                    // need to trigger the background work to asynchronously compute the effect of the updates.
                    NotifyBackgroundWorker(snapshot.CreateUpdateContext());
                }

                if (snapshot.HaveTagHelpersChanged(original))
                {
                    NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.TagHelpersChanged));
                }
            }
        }

        public override void WorkspaceProjectRemoved(Project workspaceProject)
        {
            if (workspaceProject == null)
            {
                throw new ArgumentNullException(nameof(workspaceProject));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (!IsSupportedWorkspaceProject(workspaceProject))
            {
                return;
            }

            if (_projects.TryGetValue(workspaceProject.FilePath, out var original))
            {
                // We also need to check the projectId here. If this is a multi-targeting project then we are only interested
                // in a single workspace project. Make sure the WorkspaceProject we're using is the one that's being removed.
                if (original.WorkspaceProject?.Id != workspaceProject.Id)
                {
                    return;
                }

                DefaultProjectSnapshot snapshot;

                // So if the WorkspaceProject got removed, we should double check to make sure that there aren't others
                // hanging around. This could happen if a project is multi-targeting and one of the TFMs is removed.
                var otherWorkspaceProject = GetWorkspaceProject(workspaceProject.FilePath);
                if (otherWorkspaceProject != null && otherWorkspaceProject.Id != workspaceProject.Id)
                {
                    // OK there's another WorkspaceProject, use that.
                    //
                    // Doing an update to the project should keep computed values, but mark the project as dirty if the
                    // underlying project is newer.
                    snapshot = original.WithWorkspaceProject(otherWorkspaceProject);
                    _projects[workspaceProject.FilePath] = snapshot;

                    if (snapshot.IsInitialized && snapshot.IsDirty)
                    {
                        // We don't need to notify listeners yet because we don't have any **new** computed state. However we do 
                        // need to trigger the background work to asynchronously compute the effect of the updates.
                        NotifyBackgroundWorker(snapshot.CreateUpdateContext());
                    }

                    // Notify listeners of a change because it's a different WorkspaceProject.
                    NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Changed));

                    return;
                }

                snapshot = original.RemoveWorkspaceProject();
                _projects[workspaceProject.FilePath] = snapshot;

                // Notify listeners of a change because we've removed computed state.
                NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Changed));
            }
        }

        public override void ReportError(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            _errorReporter.ReportError(exception);
        }

        public override void ReportError(Exception exception, ProjectSnapshot project)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            _errorReporter.ReportError(exception, project);
        }

        public override void ReportError(Exception exception, HostProject hostProject)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var project = hostProject?.FilePath == null ? null : this.GetProjectWithFilePath(hostProject.FilePath);
            _errorReporter.ReportError(exception, project);
        }

        public override void ReportError(Exception exception, Project workspaceProject)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            _errorReporter.ReportError(exception, workspaceProject);
        }

        // We're only interested in CSharp projects that have a FilePath. We rely on the FilePath to
        // unify the Workspace Project with our HostProject concept.
        private bool IsSupportedWorkspaceProject(Project workspaceProject) => workspaceProject.Language == LanguageNames.CSharp && workspaceProject.FilePath != null;

        private Project GetWorkspaceProject(string filePath)
        {
            var solution = Workspace.CurrentSolution;
            if (solution == null)
            {
                return null;
            }

            foreach (var workspaceProject in solution.Projects)
            {
                if (IsSupportedWorkspaceProject(workspaceProject) &&
                    FilePathComparer.Instance.Equals(filePath, workspaceProject.FilePath))
                {
                    // We don't try to handle mulitple TFMs anwhere in Razor, just take the first WorkspaceProject that is a match. 
                    return workspaceProject;
                }
            }

            return null;
        }

        // virtual so it can be overridden in tests
        protected virtual void NotifyBackgroundWorker(ProjectSnapshotUpdateContext context)
        {
            _foregroundDispatcher.AssertForegroundThread();

            _workerQueue.Enqueue(context);
        }

        // virtual so it can be overridden in tests
        protected virtual void NotifyListeners(ProjectChangeEventArgs e)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var handler = Changed;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}