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

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class RazorEditorParser : IDisposable
    {
        private AspNetCore.Razor.Language.Legacy.Span _lastChangeOwner;
        private AspNetCore.Razor.Language.Legacy.Span _lastAutoCompleteSpan;
        private BackgroundParser _parser;

        // For testing only.
        internal RazorEditorParser(RazorCodeDocument codeDocument)
        {
            CodeDocument = codeDocument;
        }

        public RazorEditorParser(RazorTemplateEngine templateEngine, string filePath)
        {
            if (templateEngine == null)
            {
                throw new ArgumentNullException(nameof(templateEngine));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(
                    AspNetCore.Razor.Language.Resources.ArgumentCannotBeNullOrEmpty,
                    nameof(filePath));
            }

            TemplateEngine = templateEngine;
            FilePath = filePath;
            _parser = new BackgroundParser(templateEngine, filePath);
            _parser.ResultsReady += (sender, args) => OnDocumentParseComplete(args);
            _parser.Start();
        }

        /// <summary>
        /// Event fired when a full reparse of the document completes.
        /// </summary>
        public event EventHandler<DocumentParseCompleteEventArgs> DocumentParseComplete;

        public RazorTemplateEngine TemplateEngine { get; }

        public string FilePath { get; }

        // Internal for testing.
        internal RazorSyntaxTree CurrentSyntaxTree { get; private set; }

        internal RazorCodeDocument CodeDocument { get; private set; }

        // Internal for testing.
        internal bool LastResultProvisional { get; private set; }

        public virtual string GetAutoCompleteString()
        {
            if (_lastAutoCompleteSpan?.EditHandler is AutoCompleteEditHandler editHandler)
            {
                return editHandler.AutoCompleteString;
            }

            return null;
        }

        public virtual PartialParseResult CheckForStructureChanges(SourceChange change, ITextSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var result = PartialParseResultInternal.Rejected;

            using (_parser.SynchronizeMainThreadState())
            {
                // Check if we can partial-parse
                if (CurrentSyntaxTree != null && _parser.IsIdle)
                {
                    result = TryPartialParse(change);
                }
            }

            // If partial parsing failed or there were outstanding parser tasks, start a full reparse
            if ((result & PartialParseResultInternal.Rejected) == PartialParseResultInternal.Rejected)
            {
                _parser.QueueChange(change, snapshot);
            }

            // Otherwise, remember if this was provisionally accepted for next partial parse
            LastResultProvisional = (result & PartialParseResultInternal.Provisional) == PartialParseResultInternal.Provisional;
            VerifyFlagsAreValid(result);

            return (PartialParseResult)result;
        }

        /// <summary>
        /// Disposes of this parser. Should be called when the editor window is closed and the document is unloaded.
        /// </summary>
        public void Dispose()
        {
            _parser.Dispose();
            GC.SuppressFinalize(this);
        }

        private PartialParseResultInternal TryPartialParse(SourceChange change)
        {
            var result = PartialParseResultInternal.Rejected;

            // Try the last change owner
            if (_lastChangeOwner != null && _lastChangeOwner.EditHandler.OwnsChange(_lastChangeOwner, change))
            {
                var editResult = _lastChangeOwner.EditHandler.ApplyChange(_lastChangeOwner, change);
                result = editResult.Result;
                if ((editResult.Result & PartialParseResultInternal.Rejected) != PartialParseResultInternal.Rejected)
                {
                    _lastChangeOwner.ReplaceWith(editResult.EditedSpan);
                }

                return result;
            }

            // Locate the span responsible for this change
            _lastChangeOwner = CurrentSyntaxTree.Root.LocateOwner(change);

            if (LastResultProvisional)
            {
                // Last change owner couldn't accept this, so we must do a full reparse
                result = PartialParseResultInternal.Rejected;
            }
            else if (_lastChangeOwner != null)
            {
                var editResult = _lastChangeOwner.EditHandler.ApplyChange(_lastChangeOwner, change);
                result = editResult.Result;
                if ((editResult.Result & PartialParseResultInternal.Rejected) != PartialParseResultInternal.Rejected)
                {
                    _lastChangeOwner.ReplaceWith(editResult.EditedSpan);
                }
                if ((result & PartialParseResultInternal.AutoCompleteBlock) == PartialParseResultInternal.AutoCompleteBlock)
                {
                    _lastAutoCompleteSpan = _lastChangeOwner;
                }
                else
                {
                    _lastAutoCompleteSpan = null;
                }
            }

            return result;
        }

        private void OnDocumentParseComplete(DocumentParseCompleteEventArgs args)
        {
            using (_parser.SynchronizeMainThreadState())
            {
                CurrentSyntaxTree = args.CodeDocument.GetSyntaxTree();
                CodeDocument = args.CodeDocument;
                _lastChangeOwner = null;
            }

            Debug.Assert(args != null, "Event arguments cannot be null");
            EventHandler<DocumentParseCompleteEventArgs> handler = DocumentParseComplete;
            if (handler != null)
            {
                try
                {
                    handler(this, args);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[RzEd] Document Parse Complete Handler Threw: " + ex.ToString());
                }
            }
        }

        [Conditional("DEBUG")]
        private static void VerifyFlagsAreValid(PartialParseResultInternal result)
        {
            Debug.Assert(((result & PartialParseResultInternal.Accepted) == PartialParseResultInternal.Accepted) ||
                         ((result & PartialParseResultInternal.Rejected) == PartialParseResultInternal.Rejected),
                         "Partial Parse result does not have either of Accepted or Rejected flags set");
            Debug.Assert(((result & PartialParseResultInternal.Rejected) == PartialParseResultInternal.Rejected) ||
                         ((result & PartialParseResultInternal.SpanContextChanged) != PartialParseResultInternal.SpanContextChanged),
                         "Partial Parse result was Accepted AND had SpanContextChanged flag set");
            Debug.Assert(((result & PartialParseResultInternal.Rejected) == PartialParseResultInternal.Rejected) ||
                         ((result & PartialParseResultInternal.AutoCompleteBlock) != PartialParseResultInternal.AutoCompleteBlock),
                         "Partial Parse result was Accepted AND had AutoCompleteBlock flag set");
            Debug.Assert(((result & PartialParseResultInternal.Accepted) == PartialParseResultInternal.Accepted) ||
                         ((result & PartialParseResultInternal.Provisional) != PartialParseResultInternal.Provisional),
                         "Partial Parse result was Rejected AND had Provisional flag set");
        }

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

            protected virtual void OnResultsReady(DocumentParseCompleteEventArgs args)
            {
                var handler = ResultsReady;
                if (handler != null)
                {
                    handler(this, args);
                }
            }

            private static bool TreesAreDifferent(RazorSyntaxTree leftTree, RazorSyntaxTree rightTree, IEnumerable<Edit> edits, CancellationToken cancelToken)
            {
                return TreesAreDifferent(leftTree.Root, rightTree.Root, edits.Select(edit => edit.Change), cancelToken);
            }

            internal static bool TreesAreDifferent(Block leftTree, Block rightTree, IEnumerable<SourceChange> changes, CancellationToken cancelToken)
            {
                // Apply all the pending changes to the original tree
                // PERF: If this becomes a bottleneck, we can probably do it the other way around,
                //  i.e. visit the tree and find applicable changes for each node.
                foreach (var change in changes)
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
                private IList<Edit> _changes = new List<Edit>();

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

                public void QueueChange(Edit edit)
                {
                    EnsureOnThread();
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
                                    DocumentParseCompleteEventArgs args = null;
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

                                                var treeStructureChanged = _currentSyntaxTree == null || TreesAreDifferent(_currentSyntaxTree, results.GetSyntaxTree(), allEdits, parcel.CancelToken);
                                                _currentSyntaxTree = results.GetSyntaxTree();

                                                // Build Arguments
                                                args = new DocumentParseCompleteEventArgs(
                                                    finalEdit.Change,
                                                    finalEdit.Snapshot,
                                                    treeStructureChanged,
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
}
