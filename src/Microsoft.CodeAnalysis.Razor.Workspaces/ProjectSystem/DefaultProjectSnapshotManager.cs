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
        private readonly Dictionary<ProjectId, ProjectSnapshot> _projects;
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

            _projects = new Dictionary<ProjectId, ProjectSnapshot>();
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

            // We need to notify listeners about every project add.
            NotifyListeners(new ProjectChangeEventArgs(snapshot, ProjectChangeKind.Added));
        }

        public override void ProjectChanged(Project underlyingProject)
        {
            if (underlyingProject == null)
            {
                throw new ArgumentNullException(nameof(underlyingProject));
            }

            // For now we don't have any state associated with the project so we can just construct a new snapshot.
            var snapshot = new DefaultProjectSnapshot(underlyingProject);
            _projects[underlyingProject.Id] = snapshot;

            // There's no need to notify listeners about project changes because we don't have any state.
            // This will change when we implement extensibility support.
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
    }
}
