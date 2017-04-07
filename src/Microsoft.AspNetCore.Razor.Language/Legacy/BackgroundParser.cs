// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class BackgroundParser : IDisposable
    {
        private MainThreadState _main;
        private BackgroundThread _bg;

        public BackgroundParser(RazorTemplateEngine templateEngine, string fileName)
        {
            _main = new MainThreadState(fileName);
            _bg = new BackgroundThread(_main, templateEngine, fileName);

            _main.ResultsReady += (sender, args) => OnResultsReady(args);
        }

        /// <summary>
        /// Fired on the main thread.
        /// </summary>
        public event EventHandler<DocumentParseCompleteEventArgs> ResultsReady;

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

        public void QueueChange(TextChange change)
        {
            _main.QueueChange(change);
        }

        public void Dispose()
        {
            _main.Cancel();
        }

        public IDisposable SynchronizeMainThreadState()
        {
            return _main.Lock();
        }

        protected virtual void OnResultsReady(DocumentParseCompleteEventArgs args)
        {
            var handler = ResultsReady;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        internal static bool TreesAreDifferent(RazorSyntaxTree leftTree, RazorSyntaxTree rightTree, IEnumerable<TextChange> changes)
        {
            return TreesAreDifferent(leftTree, rightTree, changes, CancellationToken.None);
        }

        internal static bool TreesAreDifferent(RazorSyntaxTree leftTree, RazorSyntaxTree rightTree, IEnumerable<TextChange> changes, CancellationToken cancelToken)
        {
            return TreesAreDifferent(leftTree.Root, rightTree.Root, changes, cancelToken);
        }

        internal static bool TreesAreDifferent(Block leftTree, Block rightTree, IEnumerable<TextChange> changes)
        {
            return TreesAreDifferent(leftTree, rightTree, changes, CancellationToken.None);
        }

        internal static bool TreesAreDifferent(Block leftTree, Block rightTree, IEnumerable<TextChange> changes, CancellationToken cancelToken)
        {
            // Apply all the pending changes to the original tree
            // PERF: If this becomes a bottleneck, we can probably do it the other way around,
            //  i.e. visit the tree and find applicable changes for each node.
            foreach (TextChange change in changes)
            {
                cancelToken.ThrowIfCancellationRequested();
                var changeOwner = leftTree.LocateOwner(change);

                // Apply the change to the tree
                if (changeOwner == null)
                {
                    return true;
                }
                var result = changeOwner.EditHandler.ApplyChange(changeOwner, change, force: true);
                changeOwner.ReplaceWith(result.EditedSpan);
            }

            // Now compare the trees
            var treesDifferent = !leftTree.EquivalentTo(rightTree);
            return treesDifferent;
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
            private IList<TextChange> _changes = new List<TextChange>();

            public MainThreadState(string fileName)
            {
                _fileName = fileName;

                SetThreadId(Thread.CurrentThread.ManagedThreadId);
            }

            public event EventHandler<DocumentParseCompleteEventArgs> ResultsReady;

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

            public void QueueChange(TextChange change)
            {
                EnsureOnThread();
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
                    _changes = new List<TextChange>();
                    return new WorkParcel(changes, _currentParcelCancelSource.Token);
                }
            }

            public void ReturnParcel(DocumentParseCompleteEventArgs args)
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
            private string _fileName;
            private RazorSyntaxTree _currentSyntaxTree;
            private IList<TextChange> _previouslyDiscarded = new List<TextChange>();

            public BackgroundThread(MainThreadState main, RazorTemplateEngine templateEngine, string fileName)
            {
                // Run on MAIN thread!
                _main = main;
                _shutdownToken = _main.CancelToken;
                _templateEngine = templateEngine;
                _fileName = fileName;

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
                var fileNameOnly = Path.GetFileName(_fileName);

                try
                {
                    EnsureOnThread();

#if NETSTANDARD1_3
                    var spinWait = new SpinWait();
#endif

                    while (!_shutdownToken.IsCancellationRequested)
                    {
                        // Grab the parcel of work to do
                        var parcel = _main.GetParcel();
                        if (parcel.Changes.Any())
                        {
                            try
                            {
                                DocumentParseCompleteEventArgs args = null;
                                using (var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken, parcel.CancelToken))
                                {
                                    if (!linkedCancel.IsCancellationRequested)
                                    {
                                        // Collect ALL changes
                                        List<TextChange> allChanges;

                                        if (_previouslyDiscarded != null)
                                        {
                                            allChanges = Enumerable.Concat(_previouslyDiscarded, parcel.Changes).ToList();
                                        }
                                        else
                                        {
                                            allChanges = parcel.Changes.ToList();
                                        }

                                        var finalChange = allChanges.Last();

                                        var results = ParseChange(finalChange.NewBuffer, linkedCancel.Token);

                                        if (results != null && !linkedCancel.IsCancellationRequested)
                                        {
                                            // Clear discarded changes list
                                            _previouslyDiscarded = null;

                                            var treeStructureChanged = _currentSyntaxTree == null || TreesAreDifferent(_currentSyntaxTree, results.GetSyntaxTree(), allChanges, parcel.CancelToken);
                                            _currentSyntaxTree = results.GetSyntaxTree();

                                            // Build Arguments
                                            args = new DocumentParseCompleteEventArgs()
                                            {
                                                GeneratorResults = results,
                                                SourceChange = finalChange,
                                                TreeStructureChanged = treeStructureChanged
                                            };
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
#if NETSTANDARD1_3
                            // This does the equivalent of thread.yield under the covers.
                            spinWait.SpinOnce();
#else
                            // No Yield in CoreCLR

                            Thread.Yield();
#endif
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

            private RazorCodeDocument ParseChange(ITextBuffer buffer, CancellationToken token)
            {
                EnsureOnThread();
                
                // Seek the buffer to the beginning
                buffer.Position = 0;

                var sourceDocument = LegacySourceDocument.Create(buffer, _fileName);
                var imports = _templateEngine.GetImports(_fileName);

                var codeDocument = RazorCodeDocument.Create(sourceDocument, imports);

                _templateEngine.GenerateCode(codeDocument);
                return codeDocument;
            }
        }

        private class WorkParcel
        {
            public WorkParcel(IList<TextChange> changes, CancellationToken cancelToken)
            {
                Changes = changes;
                CancelToken = cancelToken;
            }

            public CancellationToken CancelToken { get; private set; }
            public IList<TextChange> Changes { get; private set; }
        }
    }
}
