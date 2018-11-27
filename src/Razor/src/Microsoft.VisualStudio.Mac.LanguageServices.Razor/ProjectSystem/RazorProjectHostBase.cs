// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Threading;
using MonoDevelop.Projects;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor.ProjectSystem
{
    internal abstract class RazorProjectHostBase
    {
        // References changes are always triggered when project changes happen.
        private const string ProjectChangedHint = "References";

        private bool _batchingProjectChanges;
        private readonly DotNetProject _dotNetProject;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectSnapshotManagerBase _projectSnapshotManager;
        private readonly AsyncSemaphore _onProjectChangedInnerSemaphore;
        private readonly AsyncSemaphore _projectChangedSemaphore;
        private HostProject _currentHostProject;

        public RazorProjectHostBase(
            DotNetProject project,
            ForegroundDispatcher foregroundDispatcher,
            ProjectSnapshotManagerBase projectSnapshotManager)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectSnapshotManager == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManager));
            }

            _dotNetProject = project;
            _foregroundDispatcher = foregroundDispatcher;
            _projectSnapshotManager = projectSnapshotManager;
            _onProjectChangedInnerSemaphore = new AsyncSemaphore(initialCount: 1);
            _projectChangedSemaphore = new AsyncSemaphore(initialCount: 1);

            AttachToProject();
        }

        public DotNetProject DotNetProject => _dotNetProject;

        public HostProject HostProject => _currentHostProject;

        protected ForegroundDispatcher ForegroundDispatcher => _foregroundDispatcher;

        public void Detatch()
        {
            _foregroundDispatcher.AssertForegroundThread();

            DotNetProject.Modified -= DotNetProject_Modified;

            UpdateHostProjectForeground(null);
        }

        protected abstract Task OnProjectChangedAsync();

        // Protected virtual for testing
        protected virtual void AttachToProject()
        {
            ForegroundDispatcher.AssertForegroundThread();

            DotNetProject.Modified += DotNetProject_Modified;

            // Trigger the initial update to the project.
            _batchingProjectChanges = true;
            Task.Factory.StartNew(ProjectChangedBackgroundAsync, null, CancellationToken.None, TaskCreationOptions.None, ForegroundDispatcher.BackgroundScheduler);
        }

        // Must be called inside the lock.
        protected async Task UpdateHostProjectUnsafeAsync(HostProject newHostProject)
        {
            _foregroundDispatcher.AssertBackgroundThread();

            await Task.Factory.StartNew(UpdateHostProjectForeground, newHostProject, CancellationToken.None, TaskCreationOptions.None, ForegroundDispatcher.ForegroundScheduler);
        }

        protected async Task ExecuteWithLockAsync(Func<Task> func)
        {
            using (await _projectChangedSemaphore.EnterAsync().ConfigureAwait(false))
            {
                await func().ConfigureAwait(false);
            }
        }

        private async Task ProjectChangedBackgroundAsync(object state)
        {
            ForegroundDispatcher.AssertBackgroundThread();

            _batchingProjectChanges = false;

            // Ensure ordering, typically we'll only have 1 background thread in flight at a time. However,
            // between this line and the one prior another background thread could have also entered this
            // method. This is here to protect against us changing the order of project changed events.
            using (await _onProjectChangedInnerSemaphore.EnterAsync().ConfigureAwait(false))
            {
                await OnProjectChangedAsync();
            }
        }

        private void DotNetProject_Modified(object sender, SolutionItemModifiedEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            ForegroundDispatcher.AssertForegroundThread();

            if (_batchingProjectChanges)
            {
                // Already waiting to recompute host project, no need to do any more work to determine if we're dirty.
                return;
            }

            var projectChanged = args.Any(arg => string.Equals(arg.Hint, ProjectChangedHint, StringComparison.Ordinal));
            if (projectChanged)
            {
                // This method can be spammed for tons of project change events but all we really care about is "are we dirty?". 
                // Therefore, we re-dispatch here to allow any remaining project change events to fire and to then only have 1 host
                // project change trigger; this way we don't spam our own system with re-configure calls.
                _batchingProjectChanges = true;
                Task.Factory.StartNew(ProjectChangedBackgroundAsync, null, CancellationToken.None, TaskCreationOptions.None, ForegroundDispatcher.BackgroundScheduler);
            }
        }

        private void UpdateHostProjectForeground(object state)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var newHostProject = (HostProject)state;

            if (_currentHostProject == null && newHostProject == null)
            {
                // This is a no-op. This project isn't using Razor.
            }
            else if (_currentHostProject == null && newHostProject != null)
            {
                _projectSnapshotManager.HostProjectAdded(newHostProject);
            }
            else if (_currentHostProject != null && newHostProject == null)
            {
                _projectSnapshotManager.HostProjectRemoved(HostProject);
            }
            else
            {
                _projectSnapshotManager.HostProjectChanged(newHostProject);
            }

            _currentHostProject = newHostProject;
        }
    }
}
