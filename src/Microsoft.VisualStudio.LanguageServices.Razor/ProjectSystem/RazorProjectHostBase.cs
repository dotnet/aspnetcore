// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class RazorProjectHostBase : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private readonly Workspace _workspace;
        private readonly AsyncSemaphore _lock;

        private ProjectSnapshotManagerBase _projectManager;
        private HostProject _current;

        public RazorProjectHostBase(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            if (commonServices == null)
            {
                throw new ArgumentNullException(nameof(commonServices));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            CommonServices = commonServices;
            _workspace = workspace;

            _lock = new AsyncSemaphore(initialCount: 1);
        }

        // Internal for testing
        protected RazorProjectHostBase(
            IUnconfiguredProjectCommonServices commonServices,
             Workspace workspace,
             ProjectSnapshotManagerBase projectManager)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            if (commonServices == null)
            {
                throw new ArgumentNullException(nameof(commonServices));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            CommonServices = commonServices;
            _workspace = workspace;
            _projectManager = projectManager;

            _lock = new AsyncSemaphore(initialCount: 1);
        }

        protected IUnconfiguredProjectCommonServices CommonServices { get; }

        // internal for tests. The product will call through the IProjectDynamicLoadComponent interface.
        internal Task LoadAsync()
        {
            return InitializeAsync();
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            CommonServices.UnconfiguredProject.ProjectRenaming += UnconfiguredProject_ProjectRenaming;

            return Task.CompletedTask;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                CommonServices.UnconfiguredProject.ProjectRenaming -= UnconfiguredProject_ProjectRenaming;

                await ExecuteWithLock(async () =>
                {
                    if (_current != null)
                    {
                        await UpdateProjectUnsafeAsync(null).ConfigureAwait(false);
                    }
                });
            }
        }

        // Internal for tests
        internal async Task OnProjectRenamingAsync()
        {
            // When a project gets renamed we expect any rules watched by the derived class to fire.
            //
            // However, the project snapshot manager uses the project Fullpath as the key. We want to just
            // reinitialize the HostProject with the same configuration and settings here, but the updated
            // FilePath.
            await ExecuteWithLock(async () =>
            {
                if (_current != null)
                {
                    var old = _current;
                    await UpdateProjectUnsafeAsync(null).ConfigureAwait(false);

                    var filePath = CommonServices.UnconfiguredProject.FullPath;
                    await UpdateProjectUnsafeAsync(new HostProject(filePath, old.Configuration)).ConfigureAwait(false);
                }
            });
        }

        // Should only be called from the UI thread.
        private ProjectSnapshotManagerBase GetProjectManager()
        {
            CommonServices.ThreadingService.VerifyOnUIThread();

            if (_projectManager == null)
            {
                _projectManager = (ProjectSnapshotManagerBase)_workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<ProjectSnapshotManager>();
            }

            return _projectManager;
        }

        // Must be called inside the lock.
        protected async Task UpdateProjectUnsafeAsync(HostProject project)
        {
            await CommonServices.ThreadingService.SwitchToUIThread();
            var projectManager = GetProjectManager();

            if (_current == null && project == null)
            {
                // This is a no-op. This project isn't using Razor.
            }
            else if (_current == null && project != null)
            {
                projectManager.HostProjectAdded(project);
            }
            else if (_current != null && project == null)
            {
                projectManager.HostProjectRemoved(_current);
            }
            else
            {
                projectManager.HostProjectChanged(project);
            }

            _current = project;
        }
        
        protected async Task ExecuteWithLock(Func<Task> func)
        {
            using (JoinableCollection.Join())
            {
                using (await _lock.EnterAsync().ConfigureAwait(false))
                {
                    var task = JoinableFactory.RunAsync(func);
                    await task.Task.ConfigureAwait(false);
                }
            }
        }

        Task IProjectDynamicLoadComponent.LoadAsync()
        {
            return InitializeAsync();
        }

        Task IProjectDynamicLoadComponent.UnloadAsync()
        {
            return DisposeAsync();
        }

        private async Task UnconfiguredProject_ProjectRenaming(object sender, ProjectRenamedEventArgs args)
        {
            await OnProjectRenamingAsync().ConfigureAwait(false);
        }
    }
}