// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DefaultProjectSnapshotManager : ProjectSnapshotManagerBase
    {
        private readonly ProjectSnapshotChangeTrigger[] _triggers;
        private readonly Dictionary<ProjectId, DefaultProjectSnapshot> _projects;
        private readonly List<WeakReference<DefaultProjectSnapshotListener>> _listeners;

        public DefaultProjectSnapshotManager(IEnumerable<ProjectSnapshotChangeTrigger> triggers, Workspace workspace)
        {
            if (triggers == null)
            {
                throw new ArgumentNullException(nameof(triggers));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _triggers = triggers.ToArray();
            Workspace = workspace;

            _projects = new Dictionary<ProjectId, DefaultProjectSnapshot>();
            _listeners = new List<WeakReference<DefaultProjectSnapshotListener>>();

            for (var i = 0; i < _triggers.Length; i++)
            {
                _triggers[i].Initialize(this);
            }
        }

        public override IReadOnlyList<ProjectSnapshot> Projects => _projects.Values.ToArray();

        public override Workspace Workspace { get; }

        public override ProjectSnapshotListener Subscribe()
        {
            var subscription = new DefaultProjectSnapshotListener();
            _listeners.Add(new WeakReference<DefaultProjectSnapshotListener>(subscription));

            return subscription;
        }

        public override void ProjectAdded(Project underlyingProject)
        {
            if (underlyingProject == null)
            {
                throw new ArgumentNullException(nameof(underlyingProject));
            }

            var snapshot = new DefaultProjectSnapshot(underlyingProject);
            _projects[underlyingProject.Id] = snapshot;

            // New projects always start dirty, need to compute state in the background.
            NotifyBackgroundWorker();

            // We need to notify listeners about every project add.
            NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Added));
        }

        public override void ProjectChanged(Project underlyingProject)
        {
            if (underlyingProject == null)
            {
                throw new ArgumentNullException(nameof(underlyingProject));
            }

            if (_projects.TryGetValue(underlyingProject.Id, out var original))
            {
                // Doing an update to the project should keep computed values, but mark the project as dirty if the
                // underlying project is newer.
                var snapshot = original.WithProjectChange(underlyingProject);
                _projects[underlyingProject.Id] = snapshot;

                if (snapshot.IsDirty)
                {
                    // We don't need to notify listeners yet because we don't have any **new** computed state. However we do 
                    // need to trigger the background work to asynchronously compute the effect of the updates.
                    NotifyBackgroundWorker();
                }
            }
        }

        public override void ProjectChanged(ProjectSnapshotUpdateContext update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            if (_projects.TryGetValue(update.UnderlyingProject.Id, out var original))
            {
                // This is an update to the project's computed values, so everything should be overwritten
                var snapshot = original.WithProjectChange(update);
                _projects[update.UnderlyingProject.Id] = snapshot;

                if (snapshot.IsDirty)
                {
                    // It's possible that the snapshot can still be dirty if we got a project update while computing state in
                    // the background. We need to trigger the background work to asynchronously compute the effect of the updates.
                    NotifyBackgroundWorker();
                }

                // Now we need to know if the changes that we applied are significant. If that's the case then 
                // we need to notify listeners.
                if (snapshot.HasChangesComparedTo(original))
                {
                    NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Changed));
                }
            }
        }

        public override void ProjectRemoved(Project underlyingProject)
        {
            if (underlyingProject == null)
            {
                throw new ArgumentNullException(nameof(underlyingProject));
            }
            
            if (_projects.TryGetValue(underlyingProject.Id, out var snapshot))
            {
                _projects.Remove(underlyingProject.Id);

                // We need to notify listeners about every project removal.
                NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Removed));
            }
        }

        public override void ProjectsCleared()
        {
            foreach (var kvp in _projects.ToArray())
            {
                _projects.Remove(kvp.Key);

                // We need to notify listeners about every project removal.
                NotifyListeners(new ProjectChangeEventArgs(kvp.Value, ProjectChangeKind.Removed));
            }
        }

        // virtual so it can be overridden in tests
        protected virtual void NotifyBackgroundWorker()
        {

        }

        // virtual so it can be overridden in tests
        protected virtual void NotifyListeners(ProjectChangeEventArgs e)
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
    }
}
