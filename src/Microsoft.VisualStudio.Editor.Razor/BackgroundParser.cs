// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public BackgroundParser(RazorTemplateEngine templateEngine, string filePath)
        {
            _main = new MainThreadState(filePath);
            _bg = new BackgroundThread(_main, templateEngine, filePath);

            _main.ResultsReady += (sender, args) => OnResultsReady(args);
        }

        /// <summary>
        /// Fired on the main thread.
        /// </summary>
        public event EventHandler<DocumentStructureChangedEventArgs> ResultsReady;

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

        public void QueueChange(SourceChange change, ITextSnapshot snapshot)
        {
            var edit = new Edit(change, snapshot);
            _main.QueueChange(edit);
        }

        public void Dispose()
        {
            _main.Cancel();
        }

        public IDisposable SynchronizeMainThreadState()
        {
            return _main.Lock();
        }

        protected virtual void OnResultsReady(DocumentStructureChangedEventArgs args)
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
            private IList<Edit> _changes = new List<Edit>();

            public MainThreadState(string fileName)
            {
                _fileName = fileName;

                SetThreadId(Thread.CurrentThread.ManagedThreadId);
            }

            public event EventHandler<DocumentStructureChangedEventArgs> ResultsReady;

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

            public void QueueChange(Edit edit)
            {
                // Any thread can queue a change.

                lock (_stateLock)
                {
                    // CurrentParcel token source is not null ==> There's a parse underway
                    if (_currentParcelCancelSource != null)
                    {
                        _currentParcelCancelSource.Cancel();
                    }

                    _changes.Add(edit);
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
                    _changes = new List<Edit>();
                    return new WorkParcel(changes, _currentParcelCancelSource.Token);
                }
            }

            public void ReturnParcel(DocumentStructureChangedEventArgs args)
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
            private MainThreadState _main;
            private Thread _backgroundThread;
            private CancellationToken _shutdownToken;
            private RazorTemplateEngine _templateEngine;
            private string _filePath;
            private RazorSyntaxTree _currentSyntaxTree;
            private IList<Edit> _previouslyDiscarded = new List<Edit>();

            public BackgroundThread(MainThreadState main, RazorTemplateEngine templateEngine, string fileName)
            {
                // Run on MAIN thread!
                _main = main;
                _shutdownToken = _main.CancelToken;
                _templateEngine = templateEngine;
                _filePath = fileName;

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
                var fileNameOnly = Path.GetFileName(_filePath);

                try
                {
                    EnsureOnThread();

                    while (!_shutdownToken.IsCancellationRequested)
                    {
                        // Grab the parcel of work to do
                        var parcel = _main.GetParcel();
                        if (parcel.Edits.Any())
                        {
                            try
                            {
                                DocumentStructureChangedEventArgs args = null;
                                using (var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken, parcel.CancelToken))
                                {
                                    if (!linkedCancel.IsCancellationRequested)
                                    {
                                        // Collect ALL changes
                                        List<Edit> allEdits;

                                        if (_previouslyDiscarded != null)
                                        {
                                            allEdits = Enumerable.Concat(_previouslyDiscarded, parcel.Edits).ToList();
                                        }
                                        else
                                        {
                                            allEdits = parcel.Edits.ToList();
                                        }

                                        var finalEdit = allEdits.Last();

                                        var results = ParseChange(finalEdit.Snapshot, linkedCancel.Token);

                                        if (results != null && !linkedCancel.IsCancellationRequested)
                                        {
                                            // Clear discarded changes list
                                            _previouslyDiscarded = null;

                                            _currentSyntaxTree = results.GetSyntaxTree();

                                            // Build Arguments
                                            args = new DocumentStructureChangedEventArgs(
                                                finalEdit.Change,
                                                finalEdit.Snapshot,
                                                results);
                                        }
                                        else
                                        {
                                            // Parse completed but we were cancelled in the mean time. Add these to the discarded changes set
                                            _previouslyDiscarded = allEdits;
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

                var sourceDocument = new TextSnapshotSourceDocument(snapshot, _filePath);
                var imports = _templateEngine.GetImports(_filePath);

                var codeDocument = RazorCodeDocument.Create(sourceDocument, imports);

                _templateEngine.GenerateCode(codeDocument);
                return codeDocument;
            }
        }

        private class WorkParcel
        {
            public WorkParcel(IList<Edit> changes, CancellationToken cancelToken)
            {
                Edits = changes;
                CancelToken = cancelToken;
            }

            public CancellationToken CancelToken { get; }

            public IList<Edit> Edits { get; }
        }

        private class Edit
        {
            public Edit(SourceChange change, ITextSnapshot snapshot)
            {
                Change = change;
                Snapshot = snapshot;
            }

            public SourceChange Change { get; }

            public ITextSnapshot Snapshot { get; set; }
        }
    }
}
