// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class ProjectSnapshotWorkerQueue
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DefaultProjectSnapshotManager _projectManager;
        private readonly ProjectSnapshotWorker _projectWorker;

        private readonly Dictionary<string, ProjectSnapshotUpdateContext> _projects;
        private Timer _timer;

        public ProjectSnapshotWorkerQueue(ForegroundDispatcher foregroundDispatcher, DefaultProjectSnapshotManager projectManager, ProjectSnapshotWorker projectWorker)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            if (projectWorker == null)
            {
                throw new ArgumentNullException(nameof(projectWorker));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectManager = projectManager;
            _projectWorker = projectWorker;

            _projects = new Dictionary<string, ProjectSnapshotUpdateContext>(FilePathComparer.Instance);
        }

        public bool HasPendingNotifications
        {
            get
            {
                lock (_projects)
                {
                    return _projects.Count > 0;
                }
            }
        }

        // Used in unit tests to control the timer delay.
        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(2);

        public bool IsScheduledOrRunning => _timer != null;

        // Used in unit tests to ensure we can control when background work starts.
        public ManualResetEventSlim BlockBackgroundWorkStart { get; set; }

        // Used in unit tests to ensure we can know when background work finishes.
        public ManualResetEventSlim NotifyBackgroundWorkFinish { get; set; }

        // Used in unit tests to ensure we can be notified when all completes.
        public ManualResetEventSlim NotifyForegroundWorkFinish { get; set; }

        private void OnStartingBackgroundWork()
        {
            if (BlockBackgroundWorkStart != null)
            {
                BlockBackgroundWorkStart.Wait();
                BlockBackgroundWorkStart.Reset();
            }
        }

        private void OnFinishingBackgroundWork()
        {
            if (NotifyBackgroundWorkFinish != null)
            {
                NotifyBackgroundWorkFinish.Set();
            }
        }

        private void OnFinishingForegroundWork()
        {
            if (NotifyForegroundWorkFinish != null)
            {
                NotifyForegroundWorkFinish.Set();
            }
        }

        public void Enqueue(ProjectSnapshotUpdateContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _foregroundDispatcher.AssertForegroundThread();

            lock (_projects)
            {
                // We only want to store the last 'seen' version of any given project. That way when we pick one to process
                // it's always the best version to use.
                _projects[context.FilePath] = context;

                StartWorker();
            }
        }

        protected virtual void StartWorker()
        {
            // Access to the timer is protected by the lock in Enqueue and in Timer_Tick
            if (_timer == null)
            {
                // Timer will fire after a fixed delay, but only once.
                _timer = new Timer(Timer_Tick, null, Delay, Timeout.InfiniteTimeSpan);
            }
        }

        private async void Timer_Tick(object state) // Yeah I know.
        {
            try
            {
                _foregroundDispatcher.AssertBackgroundThread();

                // Timer is stopped.
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

                OnStartingBackgroundWork();

                ProjectSnapshotUpdateContext[] work;
                lock (_projects)
                {
                    work = _projects.Values.ToArray();
                    _projects.Clear();
                }

                var updates = new(ProjectSnapshotUpdateContext context, Exception exception)[work.Length];
                for (var i = 0; i < work.Length; i++)
                {
                    try
                    {
                        updates[i] = (work[i], null);
                        await _projectWorker.ProcessUpdateAsync(updates[i].context);
                    }
                    catch (Exception projectException)
                    {
                        updates[i] = (updates[i].context, projectException);
                    }
                }

                OnFinishingBackgroundWork();

                // We need to get back to the UI thread to update the project system.
                await Task.Factory.StartNew(PersistUpdates, updates, CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

                lock (_projects)
                {
                    // Resetting the timer allows another batch of work to start.
                    _timer.Dispose();
                    _timer = null;

                    // If more work came in while we were running start the worker again.
                    if (_projects.Count > 0)
                    {
                        StartWorker();
                    }
                }

                OnFinishingForegroundWork();
            }
            catch (Exception ex)
            {
                // This is something totally unexpected, let's just send it over to the workspace.
                await Task.Factory.StartNew(() => _projectManager.ReportError(ex), CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
            }
        }

        private void PersistUpdates(object state)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var updates = ((ProjectSnapshotUpdateContext context, Exception exception)[])state;

            for (var i = 0; i < updates.Length; i++)
            {
                var update = updates[i];
                if (update.exception == null)
                {
                    _projectManager.ProjectUpdated(update.context);
                }
                else
                {
                    _projectManager.ReportError(update.exception, update.context?.WorkspaceProject);
                }
            }
        }
    }
}
