// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor
{
    // Deliberately not exported for now, until this feature is working end to end.
    internal class BackgroundDocumentGenerator : ProjectSnapshotChangeTrigger
    {
        private ForegroundDispatcher _foregroundDispatcher;
        private ProjectSnapshotManagerBase _projectManager;

        private readonly Dictionary<Key, DocumentSnapshot> _files;
        private Timer _timer;

        [ImportingConstructor]
        public BackgroundDocumentGenerator(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;

            _files = new Dictionary<Key, DocumentSnapshot>();
        }

        public bool HasPendingNotifications
        {
            get
            {
                lock (_files)
                {
                    return _files.Count > 0;
                }
            }
        }

        // Used in unit tests to control the timer delay.
        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(2);

        public bool IsScheduledOrRunning => _timer != null;

        // Used in unit tests to ensure we can control when background work starts.
        public ManualResetEventSlim BlockBackgroundWorkStart { get; set; }

        // Used in unit tests to ensure we can know when background work finishes.
        public ManualResetEventSlim NotifyBackgroundWorkStarting { get; set; }

        // Used in unit tests to ensure we can control when background work completes.
        public ManualResetEventSlim BlockBackgroundWorkCompleting { get; set; }

        // Used in unit tests to ensure we can know when background work finishes.
        public ManualResetEventSlim NotifyBackgroundWorkCompleted { get; set; }

        private void OnStartingBackgroundWork()
        {
            if (BlockBackgroundWorkStart != null)
            {
                BlockBackgroundWorkStart.Wait();
                BlockBackgroundWorkStart.Reset();
            }

            if (NotifyBackgroundWorkStarting != null)
            {
                NotifyBackgroundWorkStarting.Set();
            }
        }

        private void OnCompletingBackgroundWork()
        {
            if (BlockBackgroundWorkCompleting != null)
            {
                BlockBackgroundWorkCompleting.Wait();
                BlockBackgroundWorkCompleting.Reset();
            }
        }

        private void OnCompletedBackgroundWork()
        {
            if (NotifyBackgroundWorkCompleted != null)
            {
                NotifyBackgroundWorkCompleted.Set();
            }
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
            _projectManager.Changed += ProjectManager_Changed;
        }

        protected virtual Task ProcessDocument(DocumentSnapshot document)
        {
            return document.GetGeneratedOutputAsync();
        }

        public void Enqueue(ProjectSnapshot project, DocumentSnapshot document)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _foregroundDispatcher.AssertForegroundThread();

            lock (_files)
            {
                // We only want to store the last 'seen' version of any given document. That way when we pick one to process
                // it's always the best version to use.
                _files.Add(new Key(project.FilePath, document.FilePath), document);

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

                DocumentSnapshot[] work;
                lock (_files)
                {
                    work = _files.Values.ToArray();
                    _files.Clear();
                }

                for (var i = 0; i < work.Length; i++)
                {
                    var document = work[i];
                    try
                    {
                        await ProcessDocument(document);
                    }
                    catch (Exception ex)
                    {
                        ReportError(document, ex);
                    }
                }

                OnCompletingBackgroundWork();

                lock (_files)
                {
                    // Resetting the timer allows another batch of work to start.
                    _timer.Dispose();
                    _timer = null;

                    // If more work came in while we were running start the worker again.
                    if (_files.Count > 0)
                    {
                        StartWorker();
                    }
                }

                OnCompletedBackgroundWork();
            }
            catch (Exception ex)
            {
                // This is something totally unexpected, let's just send it over to the workspace.
                await Task.Factory.StartNew(
                    () => _projectManager.ReportError(ex),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _foregroundDispatcher.ForegroundScheduler);
            }
        }

        private void ReportError(DocumentSnapshot document, Exception ex)
        {
            GC.KeepAlive(Task.Factory.StartNew(
                () => _projectManager.ReportError(ex),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler));
        }

        private void ProjectManager_Changed(object sender, ProjectChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case ProjectChangeKind.ProjectAdded:
                case ProjectChangeKind.ProjectChanged:
                case ProjectChangeKind.DocumentsChanged:
                    {
                        var project = _projectManager.GetLoadedProject(e.ProjectFilePath);
                        foreach (var documentFilePath in project.DocumentFilePaths)
                        {
                            Enqueue(project, project.GetDocument(documentFilePath));
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentContentChanged:
                    {
                        throw null;
                    }

                case ProjectChangeKind.ProjectRemoved:
                    // ignore
                    break;

                default:
                    throw new InvalidOperationException($"Unknown ProjectChangeKind {e.Kind}");
            }
        }

        private struct Key : IEquatable<Key>
        {
            public Key(string projectFilePath, string documentFilePath)
            {
                ProjectFilePath = projectFilePath;
                DocumentFilePath = documentFilePath;
            }

            public string ProjectFilePath { get; }

            public string DocumentFilePath { get; }

            public bool Equals(Key other)
            {
                return
                    FilePathComparer.Instance.Equals(ProjectFilePath, other.ProjectFilePath) &&
                    FilePathComparer.Instance.Equals(DocumentFilePath, other.DocumentFilePath);
            }

            public override bool Equals(object obj)
            {
                return obj is Key key ? Equals(key) : false;
            }

            public override int GetHashCode()
            {
                var hash = new HashCodeCombiner();
                hash.Add(ProjectFilePath, FilePathComparer.Instance);
                hash.Add(DocumentFilePath, FilePathComparer.Instance);
                return hash;
            }
        }
    }
}