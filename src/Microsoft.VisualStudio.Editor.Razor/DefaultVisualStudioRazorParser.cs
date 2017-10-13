// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;
using Timer = System.Threading.Timer;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultVisualStudioRazorParser : VisualStudioRazorParser, IDisposable
    {
        public override event EventHandler<DocumentStructureChangedEventArgs> DocumentStructureChanged;

        // Internal for testing.
        internal TimeSpan IdleDelay = TimeSpan.FromSeconds(3);
        internal Timer _idleTimer;
        internal BackgroundParser _parser;

        private readonly object IdleLock = new object();
        private readonly ICompletionBroker _completionBroker;
        private readonly IEnumerable<IContextChangedListener> _contextChangedListeners;
        private readonly VisualStudioDocumentTracker _documentTracker;
        private readonly ForegroundDispatcher _dispatcher;
        private readonly RazorTemplateEngineFactoryService _templateEngineFactory;
        private readonly ErrorReporter _errorReporter;
        private RazorSyntaxTreePartialParser _partialParser;
        private RazorTemplateEngine _templateEngine;
        private RazorCodeDocument _codeDocument;
        private ITextSnapshot _snapshot;

        // For testing only
        internal DefaultVisualStudioRazorParser(RazorCodeDocument codeDocument)
        {
            _codeDocument = codeDocument;
        }

        public DefaultVisualStudioRazorParser(
            ForegroundDispatcher dispatcher,
            VisualStudioDocumentTracker documentTracker,
            RazorTemplateEngineFactoryService templateEngineFactory,
            ErrorReporter errorReporter,
            ICompletionBroker completionBroker,
            IEnumerable<IContextChangedListener> contextChangedListeners)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (documentTracker == null)
            {
                throw new ArgumentNullException(nameof(documentTracker));
            }

            if (templateEngineFactory == null)
            {
                throw new ArgumentNullException(nameof(templateEngineFactory));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (completionBroker == null)
            {
                throw new ArgumentNullException(nameof(completionBroker));
            }

            if (contextChangedListeners == null)
            {
                throw new ArgumentNullException(nameof(contextChangedListeners));
            }

            _dispatcher = dispatcher;
            _templateEngineFactory = templateEngineFactory;
            _errorReporter = errorReporter;
            _completionBroker = completionBroker;
            _contextChangedListeners = contextChangedListeners;
            _documentTracker = documentTracker;

            _documentTracker.ContextChanged += DocumentTracker_ContextChanged;
        }

        public override RazorTemplateEngine TemplateEngine => _templateEngine;

        public override string FilePath => _documentTracker.FilePath;

        public override RazorCodeDocument CodeDocument => _codeDocument;

        public override ITextSnapshot Snapshot => _snapshot;

        public override ITextBuffer TextBuffer => _documentTracker.TextBuffer;

        // Used in unit tests to ensure we can be notified when idle starts.
        internal ManualResetEventSlim NotifyForegroundIdleStart { get; set; }

        // Used in unit tests to ensure we can block background idle work.
        internal ManualResetEventSlim BlockBackgroundIdleWork { get; set; }

        public override async Task ReparseAsync()
        {
            // Can be called from any thread

            if (_dispatcher.IsForegroundThread)
            {
                ReparseOnForeground(null);
            }
            else
            {
                await Task.Factory.StartNew(ReparseOnForeground, null, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
        }

        public void Dispose()
        {
            _dispatcher.AssertForegroundThread();

            StopParser();

            _documentTracker.ContextChanged -= DocumentTracker_ContextChanged;

            StopIdleTimer();
        }

        // Internal for testing
        internal void DocumentTracker_ContextChanged(object sender, EventArgs args)
        {
            _dispatcher.AssertForegroundThread();

            if (!TryReinitializeParser())
            {
                return;
            }

            NotifyParserContextChanged();

            // We have a new parser, force a reparse to generate new document information. Note that this
            // only blocks until the reparse change has been queued.
            ReparseAsync().GetAwaiter().GetResult();
        }

        // Internal for testing
        internal bool TryReinitializeParser()
        {
            _dispatcher.AssertForegroundThread();

            StopParser();

            if (!_documentTracker.IsSupportedProject)
            {
                // Tracker is either starting up, tearing down or wrongfully instantiated.
                // Either way, the tracker can't act on its associated project, neither can we.
                return false;
            }

            StartParser();

            return true;
        }

        // Internal for testing
        internal void NotifyParserContextChanged()
        {
            _dispatcher.AssertForegroundThread();

            // This is temporary until we own the TagHelper resolution system. At that point the parser will push out updates
            // via DocumentStructureChangedEvents when contexts change. For now, listeners need to know more information about
            // the parser. In the case that the tracker does not belong to a supported project the editor will tear down its
            // attachment to the parser when it recognizes the document closing.
            foreach (var contextChangeListener in _contextChangedListeners)
            {
                contextChangeListener.OnContextChanged(this);
            }
        }

        // Internal for testing
        internal void StartParser()
        {
            _dispatcher.AssertForegroundThread();

            var projectDirectory = Path.GetDirectoryName(_documentTracker.ProjectPath);
            _templateEngine = _templateEngineFactory.Create(projectDirectory, ConfigureTemplateEngine);
            _parser = new BackgroundParser(TemplateEngine, FilePath);
            _parser.ResultsReady += OnResultsReady;
            _parser.Start();

            TextBuffer.Changed += TextBuffer_OnChanged;
        }

        // Internal for testing
        internal void StopParser()
        {
            _dispatcher.AssertForegroundThread();

            if (_parser != null)
            {
                // Detatch from the text buffer until we have a new parser to handle changes.
                TextBuffer.Changed -= TextBuffer_OnChanged;

                _parser.ResultsReady -= OnResultsReady;
                _parser.Dispose();
                _parser = null;
            }
        }

        // Internal for testing
        internal void StartIdleTimer()
        {
            _dispatcher.AssertForegroundThread();

            lock (IdleLock)
            {
                if (_idleTimer == null)
                {
                    // Timer will fire after a fixed delay, but only once.
                    _idleTimer = new Timer(Timer_Tick, null, IdleDelay, Timeout.InfiniteTimeSpan);
                }
            }
        }

        // Internal for testing
        internal void StopIdleTimer()
        {
            // Can be called from any thread.

            lock (IdleLock)
            {
                if (_idleTimer != null)
                {
                    _idleTimer.Dispose();
                    _idleTimer = null;
                }
            }
        }

        private void TextBuffer_OnChanged(object sender, TextContentChangedEventArgs args)
        {
            _dispatcher.AssertForegroundThread();

            if (args.Changes.Count > 0)
            {
                // Idle timers are used to track provisional changes. Provisional changes only last for a single text change. After that normal
                // partial parsing rules apply (stop the timer).
                StopIdleTimer();
            }

            if (!args.TextChangeOccurred(out var changeInformation))
            {
                return;
            }

            var change = new SourceChange(changeInformation.firstChange.OldPosition, changeInformation.oldText.Length, changeInformation.newText);
            var snapshot = args.After;
            var result = PartialParseResultInternal.Rejected;

            using (_parser.SynchronizeMainThreadState())
            {
                // Check if we can partial-parse
                if (_partialParser != null && _parser.IsIdle)
                {
                    result = _partialParser.Parse(change);
                }
            }

            // If partial parsing failed or there were outstanding parser tasks, start a full reparse
            if ((result & PartialParseResultInternal.Rejected) == PartialParseResultInternal.Rejected)
            {
                _parser.QueueChange(change, snapshot);
            }

            if ((result & PartialParseResultInternal.Provisional) == PartialParseResultInternal.Provisional)
            {
                StartIdleTimer();
            }
        }

        private void OnIdle(object state)
        {
            _dispatcher.AssertForegroundThread();

            OnNotifyForegroundIdle();

            foreach (var textView in _documentTracker.TextViews)
            {
                if (_completionBroker.IsCompletionActive(textView))
                {
                    // Completion list is still active, need to re-start timer.
                    StartIdleTimer();
                    return;
                }
            }

            // This only blocks until the reparse change has been queued.
            ReparseAsync().GetAwaiter().GetResult();
        }

        private void ReparseOnForeground(object state)
        {
            _dispatcher.AssertForegroundThread();

            if (_parser == null)
            {
                Debug.Fail("Reparse being attempted after the parser has been disposed.");
                return;
            }

            var snapshot = TextBuffer.CurrentSnapshot;
            _parser.QueueChange(null, snapshot);
        }

        private void OnNotifyForegroundIdle()
        {
            if (NotifyForegroundIdleStart != null)
            {
                NotifyForegroundIdleStart.Set();
            }
        }

        private void OnStartingBackgroundIdleWork()
        {
            if (BlockBackgroundIdleWork != null)
            {
                BlockBackgroundIdleWork.Wait();
            }
        }

        private async void Timer_Tick(object state)
        {
            try
            {
                _dispatcher.AssertBackgroundThread();

                OnStartingBackgroundIdleWork();

                StopIdleTimer();

                // We need to get back to the UI thread to properly check if a completion is active.
                await Task.Factory.StartNew(OnIdle, null, CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
            catch (Exception ex)
            {
                // This is something totally unexpected, let's just send it over to the workspace.
                await Task.Factory.StartNew(() => _errorReporter.ReportError(ex), CancellationToken.None, TaskCreationOptions.None, _dispatcher.ForegroundScheduler);
            }
        }

        private void OnResultsReady(object sender, DocumentStructureChangedEventArgs args)
        {
            _dispatcher.AssertBackgroundThread();

            if (DocumentStructureChanged != null)
            {
                if (args.Snapshot != TextBuffer.CurrentSnapshot)
                {
                    // A different text change is being parsed.
                    return;
                }

                _codeDocument = args.CodeDocument;
                _snapshot = args.Snapshot;
                _partialParser = new RazorSyntaxTreePartialParser(CodeDocument.GetSyntaxTree());
                DocumentStructureChanged(this, args);
            }
        }

        private void ConfigureTemplateEngine(IRazorEngineBuilder builder)
        {
            builder.Features.Add(new VisualStudioParserOptionsFeature());
            builder.Features.Add(new VisualStudioTagHelperFeature(TextBuffer));
        }

        /// <summary>
        /// This class will cease to be useful once we harvest/monitor settings from the editor.
        /// </summary>
        private class VisualStudioParserOptionsFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                options.IndentSize = 4;
                options.IndentWithTabs = false;
            }
        }

        /// <summary>
        /// This class will cease to be useful once we control TagHelper discovery. For now, it delegates discovery
        /// to ITagHelperFeature's that exist on the text buffer.
        /// </summary>
        private class VisualStudioTagHelperFeature : ITagHelperFeature
        {
            private readonly ITextBuffer _textBuffer;

            public VisualStudioTagHelperFeature(ITextBuffer textBuffer)
            {
                _textBuffer = textBuffer;
            }

            public RazorEngine Engine { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
            {
                if (_textBuffer.Properties.TryGetProperty(typeof(ITagHelperFeature), out ITagHelperFeature feature))
                {
                    return feature.GetDescriptors();
                }

                return Array.Empty<TagHelperDescriptor>();
            }
        }
    }
}
