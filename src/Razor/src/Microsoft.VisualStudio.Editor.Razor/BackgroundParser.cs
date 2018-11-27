// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class BackgroundParser : IDisposable
    {
        private MainThreadState _main;
        private BackgroundThread _bg;

        public BackgroundParser(RazorProjectEngine projectEngine, string filePath, string projectDirectory)
        {
            _main = new MainThreadState(filePath);
            _bg = new BackgroundThread(_main, projectEngine, filePath, projectDirectory);

            _main.ResultsReady += (sender, args) => OnResultsReady(args);
        }

        /// <summary>
        /// Fired on the main thread.
        /// </summary>
        public event EventHandler<BackgroundParserResultsReadyEventArgs> ResultsReady;

        public bool IsIdle
        {
            get { return _main.IsIdle; }
        }

        public void Start()
        {
            _bg.Start();
        }

        public void Cancel()
        {
            _main.Cancel();
        }

        public ChangeReference QueueChange(SourceChange change, ITextSnapshot snapshot)
        {
            var changeReference = new ChangeReference(change, snapshot);
            _main.QueueChange(changeReference);
            return changeReference;
        }

        public void Dispose()
        {
            _main.Cancel();
        }

        public IDisposable SynchronizeMainThreadState()
        {
            return _main.Lock();
        }

        protected virtual void OnResultsReady(BackgroundParserResultsReadyEventArgs args)
        {
            using (SynchronizeMainThreadState())
            {
                ResultsReady?.Invoke(this, args);
            }
        }

        private abstract class ThreadStateBase
        {
#if DEBUG
            private int _id = -1;
#endif
            protected ThreadStateBase()
            {
            }

            [Conditional("DEBUG")]
            protected void SetThreadId(int id)
            {
#if DEBUG
                _id = id;
#endif
            }

            [Conditional("DEBUG")]
            protected void EnsureOnThread()
            {
#if DEBUG
                Debug.Assert(_id != -1, "SetThreadId was never called!");
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == _id, "Called from an unexpected thread!");
#endif
            }

            [Conditional("DEBUG")]
            protected void EnsureNotOnThread()
            {
#if DEBUG
                Debug.Assert(_id != -1, "SetThreadId was never called!");
                Debug.Assert(Thread.CurrentThread.ManagedThreadId != _id, "Called from an unexpected thread!");
#endif
            }
        }

        private class MainThreadState : ThreadStateBase, IDisposable
        {
            private readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();
            private readonly ManualResetEventSlim _hasParcel = new ManualResetEventSlim(false);
            private CancellationTokenSource _currentParcelCancelSource;

            private string _fileName;
            private readonly object _stateLock = new object();
            private IList<ChangeReference> _changes = new List<ChangeReference>();

            public MainThreadState(string fileName)
            {
                _fileName = fileName;

                SetThreadId(Thread.CurrentThread.ManagedThreadId);
            }

            public event EventHandler<BackgroundParserResultsReadyEventArgs> ResultsReady;

            public CancellationToken CancelToken
            {
                get { return _cancelSource.Token; }
            }

            public bool IsIdle
            {
                get
                {
                    lock (_stateLock)
                    {
                        return _currentParcelCancelSource == null;
                    }
                }
            }

            public void Cancel()
            {
                EnsureOnThread();
                _cancelSource.Cancel();
            }

            public IDisposable Lock()
            {
                Monitor.Enter(_stateLock);
                return new DisposableAction(() => Monitor.Exit(_stateLock));
            }

            public void QueueChange(ChangeReference change)
            {
                // Any thread can queue a change.

                lock (_stateLock)
                {
                    // CurrentParcel token source is not null ==> There's a parse underway
                    if (_currentParcelCancelSource != null)
                    {
                        _currentParcelCancelSource.Cancel();
                    }

                    _changes.Add(change);
                    _hasParcel.Set();
                }
            }

            public WorkParcel GetParcel()
            {
                EnsureNotOnThread(); // Only the background thread can get a parcel
                _hasParcel.Wait(_cancelSource.Token);
                _hasParcel.Reset();
                lock (_stateLock)
                {
                    // Create a cancellation source for this parcel
                    _currentParcelCancelSource = new CancellationTokenSource();

                    var changes = _changes;
                    _changes = new List<ChangeReference>();
                    return new WorkParcel(changes, _currentParcelCancelSource.Token);
                }
            }

            public void ReturnParcel(BackgroundParserResultsReadyEventArgs args)
            {
                lock (_stateLock)
                {
                    // Clear the current parcel cancellation source
                    if (_currentParcelCancelSource != null)
                    {
                        _currentParcelCancelSource.Dispose();
                        _currentParcelCancelSource = null;
                    }

                    // If there are things waiting to be parsed, just don't fire the event because we're already out of date
                    if (_changes.Any())
                    {
                        return;
                    }
                }
                var handler = ResultsReady;
                if (handler != null)
                {
                    handler(this, args);
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_currentParcelCancelSource != null)
                    {
                        _currentParcelCancelSource.Dispose();
                        _currentParcelCancelSource = null;
                    }
                    _cancelSource.Dispose();
                    _hasParcel.Dispose();
                }
            }
        }

        private class BackgroundThread : ThreadStateBase
        {
            private readonly string _filePath;
            private readonly string _relativeFilePath;
            private readonly string _projectDirectory;
            private MainThreadState _main;
            private Thread _backgroundThread;
            private CancellationToken _shutdownToken;
            private RazorProjectEngine _projectEngine;
            private RazorSyntaxTree _currentSyntaxTree;
            private IList<ChangeReference> _previouslyDiscarded = new List<ChangeReference>();

            public BackgroundThread(MainThreadState main, RazorProjectEngine projectEngine, string filePath, string projectDirectory)
            {
                // Run on MAIN thread!
                _main = main;
                _shutdownToken = _main.CancelToken;
                _projectEngine = projectEngine;
                _filePath = filePath;
                _relativeFilePath = GetNormalizedRelativeFilePath(filePath, projectDirectory);
                _projectDirectory = projectDirectory;
                _backgroundThread = new Thread(WorkerLoop);
                SetThreadId(_backgroundThread.ManagedThreadId);
            }

            // **** ANY THREAD ****
            public void Start()
            {
                _backgroundThread.Start();
            }

            // **** BACKGROUND THREAD ****
            private void WorkerLoop()
            {
                try
                {
                    EnsureOnThread();

                    while (!_shutdownToken.IsCancellationRequested)
                    {
                        // Grab the parcel of work to do
                        var parcel = _main.GetParcel();
                        if (parcel.Changes.Any())
                        {
                            try
                            {
                                BackgroundParserResultsReadyEventArgs args = null;
                                using (var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken, parcel.CancelToken))
                                {
                                    if (!linkedCancel.IsCancellationRequested)
                                    {
                                        // Collect ALL changes
                                        List<ChangeReference> allChanges;

                                        if (_previouslyDiscarded != null)
                                        {
                                            allChanges = Enumerable.Concat(_previouslyDiscarded, parcel.Changes).ToList();
                                        }
                                        else
                                        {
                                            allChanges = parcel.Changes.ToList();
                                        }

                                        var finalChange = allChanges.Last();

                                        var results = ParseChange(finalChange.Snapshot, linkedCancel.Token);

                                        if (results != null && !linkedCancel.IsCancellationRequested)
                                        {
                                            // Clear discarded changes list
                                            _previouslyDiscarded = null;

                                            _currentSyntaxTree = results.GetSyntaxTree();

                                            // Build Arguments
                                            args = new BackgroundParserResultsReadyEventArgs(finalChange, results);
                                        }
                                        else
                                        {
                                            // Parse completed but we were cancelled in the mean time. Add these to the discarded changes set
                                            _previouslyDiscarded = allChanges;
                                        }
                                    }
                                }
                                if (args != null)
                                {
                                    _main.ReturnParcel(args);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                            }
                        }
                        else
                        {
                            Thread.Yield();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Do nothing. Just shut down.
                }
                finally
                {
                    // Clean up main thread resources
                    _main.Dispose();
                }
            }

            private RazorCodeDocument ParseChange(ITextSnapshot snapshot, CancellationToken token)
            {
                EnsureOnThread();

                var projectItem = new TextSnapshotProjectItem(snapshot, _projectDirectory, _relativeFilePath, _filePath);
                var codeDocument = _projectEngine.ProcessDesignTime(projectItem);

                return codeDocument;
            }

            private string GetNormalizedRelativeFilePath(string filePath, string projectDirectory)
            {
                if (filePath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    filePath = filePath.Substring(projectDirectory.Length);
                }

                if (filePath.Length > 1)
                {
                    filePath = filePath.Replace('\\', '/');

                    if (filePath[0] != '/')
                    {
                        filePath = "/" + filePath;
                    }
                }

                return filePath;
            }
        }

        private class WorkParcel
        {
            public WorkParcel(IList<ChangeReference> changes, CancellationToken cancelToken)
            {
                Changes = changes;
                CancelToken = cancelToken;
            }

            public CancellationToken CancelToken { get; }

            public IList<ChangeReference> Changes { get; }
        }

        internal class ChangeReference
        {
            public ChangeReference(SourceChange change, ITextSnapshot snapshot)
            {
                Change = change;
                Snapshot = snapshot;
            }

            public SourceChange Change { get; }

            public ITextSnapshot Snapshot { get; }
        }
    }
}
