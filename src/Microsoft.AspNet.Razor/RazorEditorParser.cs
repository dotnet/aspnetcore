// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// Parser used by editors to avoid reparsing the entire document on each text change
    /// </summary>
    /// <remarks>
    /// This parser is designed to allow editors to avoid having to worry about incremental parsing.
    /// The CheckForStructureChanges method can be called with every change made by a user in an editor and
    /// the parser will provide a result indicating if it was able to incrementally reparse the document.
    /// 
    /// The general workflow for editors with this parser is:
    /// 0. User edits document
    /// 1. Editor builds TextChange structure describing the edit and providing a reference to the _updated_ text buffer
    /// 2. Editor calls CheckForStructureChanges passing in that change.
    /// 3. Parser determines if the change can be simply applied to an existing parse tree node
    ///   a.  If it can, the Parser updates its parse tree and returns PartialParseResult.Accepted
    ///   b.  If it can not, the Parser starts a background parse task and return PartialParseResult.Rejected
    /// NOTE: Additional flags can be applied to the PartialParseResult, see that enum for more details.  However,
    ///       the Accepted or Rejected flags will ALWAYS be present
    /// 
    /// A change can only be incrementally parsed if a single, unique, Span (see Microsoft.AspNet.Razor.Parser.SyntaxTree) in the syntax tree can
    /// be identified as owning the entire change.  For example, if a change overlaps with multiple spans, the change cannot be
    /// parsed incrementally and a full reparse is necessary.  A Span "owns" a change if the change occurs either a) entirely
    /// within it's boundaries or b) it is a pure insertion (see TextChange) at the end of a Span whose CanGrow flag (see Span) is
    /// true.
    /// 
    /// Even if a single unique Span owner can be identified, it's possible the edit will cause the Span to split or merge with other
    /// Spans, in which case, a full reparse is necessary to identify the extent of the changes to the tree.
    /// 
    /// When the RazorEditorParser returns Accepted, it updates CurrentParseTree immediately.  However, the editor is expected to
    /// update it's own data structures independently.  It can use CurrentParseTree to do this, as soon as the editor returns from
    /// CheckForStructureChanges, but it should (ideally) have logic for doing so without needing the new tree.
    /// 
    /// When Rejected is returned by CheckForStructureChanges, a background parse task has _already_ been started.  When that task
    /// finishes, the DocumentStructureChanged event will be fired containing the new generated code, parse tree and a reference to
    /// the original TextChange that caused the reparse, to allow the editor to resolve the new tree against any changes made since 
    /// calling CheckForStructureChanges.
    /// 
    /// If a call to CheckForStructureChanges occurs while a reparse is already in-progress, the reparse is cancelled IMMEDIATELY
    /// and Rejected is returned without attempting to reparse.  This means that if a conusmer calls CheckForStructureChanges, which
    /// returns Rejected, then calls it again before DocumentParseComplete is fired, it will only recieve one DocumentParseComplete
    /// event, for the second change.
    /// </remarks>
    public class RazorEditorParser : IDisposable
    {
        // Lock for this document
        private Span _lastChangeOwner;
        private Span _lastAutoCompleteSpan;
        private BackgroundParser _parser;
        private Block _currentParseTree;

        /// <summary>
        /// Constructs the editor parser.  One instance should be used per active editor.  This
        /// instance _can_ be shared among reparses, but should _never_ be shared between documents.
        /// </summary>
        /// <param name="host">The <see cref="RazorEngineHost"/> which defines the environment in which the generated code will live.  <see cref="F:RazorEngineHost.DesignTimeMode"/> should be set if design-time code mappings are desired</param>
        /// <param name="sourceFileName">The physical path to use in line pragmas</param>
        public RazorEditorParser(RazorEngineHost host, string sourceFileName)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }
            if (String.IsNullOrEmpty(sourceFileName))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "sourceFileName");
            }

            Host = host;
            FileName = sourceFileName;
            _parser = new BackgroundParser(host, sourceFileName);
            _parser.ResultsReady += (sender, args) => OnDocumentParseComplete(args);
            _parser.Start();
        }

        /// <summary>
        /// Event fired when a full reparse of the document completes
        /// </summary>
        public event EventHandler<DocumentParseCompleteEventArgs> DocumentParseComplete;

        public RazorEngineHost Host { get; private set; }
        public string FileName { get; private set; }
        public bool LastResultProvisional { get; private set; }
        public Block CurrentParseTree
        {
            get { return _currentParseTree; }
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Since this method is heavily affected by side-effects, particularly calls to CheckForStructureChanges, it should not be made into a property")]
        public virtual string GetAutoCompleteString()
        {
            if (_lastAutoCompleteSpan != null)
            {
                AutoCompleteEditHandler editHandler = _lastAutoCompleteSpan.EditHandler as AutoCompleteEditHandler;
                if (editHandler != null)
                {
                    return editHandler.AutoCompleteString;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines if a change will cause a structural change to the document and if not, applies it to the existing tree.
        /// If a structural change would occur, automatically starts a reparse
        /// </summary>
        /// <remarks>
        /// NOTE: The initial incremental parsing check and actual incremental parsing (if possible) occurs
        /// on the callers thread.  However, if a full reparse is needed, this occurs on a background thread.
        /// </remarks>
        /// <param name="change">The change to apply to the parse tree</param>
        /// <returns>A PartialParseResult value indicating the result of the incremental parse</returns>
        public virtual PartialParseResult CheckForStructureChanges(TextChange change)
        {
            // Validate the change
            long? elapsedMs = null;
#if EDITOR_TRACING
            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif
            RazorEditorTrace.TraceLine(RazorResources.FormatTrace_EditorReceivedChange(Path.GetFileName(FileName), change));
            if (change.NewBuffer == null)
            {
                throw new ArgumentException(RazorResources.FormatStructure_Member_CannotBeNull(
                                                          "Buffer",
                                                          "TextChange"), "change");
            }

            PartialParseResult result = PartialParseResult.Rejected;

            // If there isn't already a parse underway, try partial-parsing
            string changeString = String.Empty;
            using (_parser.SynchronizeMainThreadState())
            {
                // Capture the string value of the change while we're synchronized
                changeString = change.ToString();

                // Check if we can partial-parse
                if (CurrentParseTree != null && _parser.IsIdle)
                {
                    result = TryPartialParse(change);
                }
            }

            // If partial parsing failed or there were outstanding parser tasks, start a full reparse
            if (result.HasFlag(PartialParseResult.Rejected))
            {
                _parser.QueueChange(change);
            }

            // Otherwise, remember if this was provisionally accepted for next partial parse
            LastResultProvisional = result.HasFlag(PartialParseResult.Provisional);
            VerifyFlagsAreValid(result);

#if EDITOR_TRACING
            sw.Stop();
            elapsedMs = sw.ElapsedMilliseconds;
            sw.Reset();
#endif
            RazorEditorTrace.TraceLine(
                RazorResources.FormatTrace_EditorProcessedChange(
                            Path.GetFileName(FileName), 
                            changeString, elapsedMs.HasValue ? elapsedMs.Value.ToString(CultureInfo.InvariantCulture) : "?", 
                            result.ToString()));
            return result;
        }

        /// <summary>
        /// Disposes of this parser.  Should be called when the editor window is closed and the document is unloaded.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_cancelTokenSource", Justification = "The cancellation token is owned by the worker thread, so it is disposed there")]
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_changeReceived", Justification = "The change received event is owned by the worker thread, so it is disposed there")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _parser.Dispose();
            }
        }

        private PartialParseResult TryPartialParse(TextChange change)
        {
            PartialParseResult result = PartialParseResult.Rejected;

            // Try the last change owner
            if (_lastChangeOwner != null && _lastChangeOwner.EditHandler.OwnsChange(_lastChangeOwner, change))
            {
                EditResult editResult = _lastChangeOwner.EditHandler.ApplyChange(_lastChangeOwner, change);
                result = editResult.Result;
                if (!editResult.Result.HasFlag(PartialParseResult.Rejected))
                {
                    _lastChangeOwner.ReplaceWith(editResult.EditedSpan);
                }

                return result;
            }

            // Locate the span responsible for this change
            _lastChangeOwner = CurrentParseTree.LocateOwner(change);

            if (LastResultProvisional)
            {
                // Last change owner couldn't accept this, so we must do a full reparse
                result = PartialParseResult.Rejected;
            }
            else if (_lastChangeOwner != null)
            {
                EditResult editRes = _lastChangeOwner.EditHandler.ApplyChange(_lastChangeOwner, change);
                result = editRes.Result;
                if (!editRes.Result.HasFlag(PartialParseResult.Rejected))
                {
                    _lastChangeOwner.ReplaceWith(editRes.EditedSpan);
                }
                if (result.HasFlag(PartialParseResult.AutoCompleteBlock))
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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are being caught here intentionally")]
        private void OnDocumentParseComplete(DocumentParseCompleteEventArgs args)
        {
            using (_parser.SynchronizeMainThreadState())
            {
                _currentParseTree = args.GeneratorResults.Document;
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
        private static void VerifyFlagsAreValid(PartialParseResult result)
        {
            Debug.Assert(result.HasFlag(PartialParseResult.Accepted) ||
                         result.HasFlag(PartialParseResult.Rejected),
                         "Partial Parse result does not have either of Accepted or Rejected flags set");
            Debug.Assert(result.HasFlag(PartialParseResult.Rejected) ||
                         !result.HasFlag(PartialParseResult.SpanContextChanged),
                         "Partial Parse result was Accepted AND had SpanContextChanged flag set");
            Debug.Assert(result.HasFlag(PartialParseResult.Rejected) ||
                         !result.HasFlag(PartialParseResult.AutoCompleteBlock),
                         "Partial Parse result was Accepted AND had AutoCompleteBlock flag set");
            Debug.Assert(result.HasFlag(PartialParseResult.Accepted) ||
                         !result.HasFlag(PartialParseResult.Provisional),
                         "Partial Parse result was Rejected AND had Provisional flag set");
        }
    }
}
