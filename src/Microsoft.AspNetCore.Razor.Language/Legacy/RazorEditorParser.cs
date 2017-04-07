// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    /// <summary>
    /// Parser used by editors to avoid reparsing the entire document on each text change.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This parser is designed to allow editors to avoid having to worry about incremental parsing.
    /// The <see cref="CheckForStructureChanges"/> method can be called with every change made by a user in an editor
    /// and the parser will provide a result indicating if it was able to incrementally apply the change.
    /// </para>
    /// <para>
    /// The general workflow for editors with this parser is:
    /// <list type="number">
    /// <item><description>User edits document.</description></item>
    /// <item><description>Editor builds a <see cref="TextChange"/> structure describing the edit and providing a
    /// reference to the <em>updated</em> text buffer.</description></item>
    /// <item><description>Editor calls <see cref="CheckForStructureChanges"/> passing in that change.
    /// </description></item>
    /// <item><description>Parser determines if the change can be simply applied to an existing parse tree node.
    /// </description></item>
    /// <list type="number">
    /// <item><description>If it can, the Parser updates its parse tree and returns
    /// <see cref="PartialParseResult.Accepted"/>.</description></item>
    /// <item><description>If it cannot, the Parser starts a background parse task and returns
    /// <see cref="PartialParseResult.Rejected"/>.</description></item>
    /// </list>
    /// </list>
    /// NOTE: Additional flags can be applied to the <see cref="PartialParseResult"/>, see that <c>enum</c> for more
    /// details. However, the <see cref="PartialParseResult.Accepted"/> or <see cref="PartialParseResult.Rejected"/>
    /// flags will ALWAYS be present.
    /// </para>
    /// <para>
    /// A change can only be incrementally parsed if a single, unique, span (seein the syntax tree can be identified 
    /// as owning the entire change. For example, if a change overlaps with multiple <see cref="Span"/>s, the change 
    /// cannot be parsed incrementally and a full reparse is necessary. A <see cref="Span"/> "owns" a change if the
    /// change occurs either a) entirely within it's boundaries or b) it is a pure insertion 
    /// (see <see cref="TextChange"/>) at the end of a <see cref="Span"/> whose <see cref="Span.EditHandler"/> can 
    /// accept the change (see <see cref="SpanEditHandler.CanAcceptChange"/>).
    /// </para>
    /// <para>
    /// When the <see cref="RazorEditorParser"/> returns <see cref="PartialParseResult.Accepted"/>, it updates
    /// <see cref="CurrentSyntaxTree"/> immediately. However, the editor is expected to update it's own data structures
    /// independently. It can use <see cref="CurrentSyntaxTree"/> to do this, as soon as the editor returns from
    /// <see cref="CheckForStructureChanges"/>, but it should (ideally) have logic for doing so without needing the new
    /// tree.
    /// </para>
    /// <para>
    /// When <see cref="PartialParseResult.Rejected"/> is returned by <see cref="CheckForStructureChanges"/>, a
    /// background parse task has <em>already</em> been started. When that task finishes, the
    /// <see cref="DocumentParseComplete"/> event will be fired containing the new generated code, parse tree and a
    /// reference to the original <see cref="TextChange"/> that caused the reparse, to allow the editor to resolve the
    /// new tree against any changes made since calling <see cref="CheckForStructureChanges"/>.
    /// </para>
    /// <para>
    /// If a call to <see cref="CheckForStructureChanges"/> occurs while a reparse is already in-progress, the reparse
    /// is canceled IMMEDIATELY and <see cref="PartialParseResult.Rejected"/> is returned without attempting to
    /// reparse. This means that if a consumer calls <see cref="CheckForStructureChanges"/>, which returns
    /// <see cref="PartialParseResult.Rejected"/>, then calls it again before <see cref="DocumentParseComplete"/> is
    /// fired, it will only receive one <see cref="DocumentParseComplete"/> event, for the second change.
    /// </para>
    /// </remarks>
    public class RazorEditorParser : IDisposable
    {
        // Lock for this document
        private Span _lastChangeOwner;
        private Span _lastAutoCompleteSpan;
        private BackgroundParser _parser;
        private RazorSyntaxTree _currentSyntaxTree;

        public RazorEditorParser(RazorTemplateEngine templateEngine, string sourceFileName)
        {
            if (templateEngine == null)
            {
                throw new ArgumentNullException(nameof(templateEngine));
            }

            if (string.IsNullOrEmpty(sourceFileName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(sourceFileName));
            }

            TemplateEngine = templateEngine;
            FileName = sourceFileName;
            _parser = new BackgroundParser(templateEngine, sourceFileName);
            _parser.ResultsReady += (sender, args) => OnDocumentParseComplete(args);
            _parser.Start();
        }

        /// <summary>
        /// Event fired when a full reparse of the document completes.
        /// </summary>
        public event EventHandler<DocumentParseCompleteEventArgs> DocumentParseComplete;

        public RazorTemplateEngine TemplateEngine { get; private set; }
        public string FileName { get; private set; }
        public bool LastResultProvisional { get; private set; }

        public RazorSyntaxTree CurrentSyntaxTree => _currentSyntaxTree; 

        public virtual string GetAutoCompleteString()
        {
            if (_lastAutoCompleteSpan != null)
            {
                var editHandler = _lastAutoCompleteSpan.EditHandler as AutoCompleteEditHandler;
                if (editHandler != null)
                {
                    return editHandler.AutoCompleteString;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines if a change will cause a structural change to the document and if not, applies it to the
        /// existing tree. If a structural change would occur, automatically starts a reparse.
        /// </summary>
        /// <remarks>
        /// NOTE: The initial incremental parsing check and actual incremental parsing (if possible) occurs
        /// on the caller's thread. However, if a full reparse is needed, this occurs on a background thread.
        /// </remarks>
        /// <param name="change">The change to apply to the parse tree.</param>
        /// <returns>A <see cref="PartialParseResult"/> value indicating the result of the incremental parse.</returns>
        public virtual PartialParseResult CheckForStructureChanges(TextChange change)
        {
            var result = PartialParseResult.Rejected;

            using (_parser.SynchronizeMainThreadState())
            {
                // Check if we can partial-parse
                if (CurrentSyntaxTree != null && _parser.IsIdle)
                {
                    result = TryPartialParse(change);
                }
            }

            // If partial parsing failed or there were outstanding parser tasks, start a full reparse
            if ((result & PartialParseResult.Rejected) == PartialParseResult.Rejected)
            {
                _parser.QueueChange(change);
            }

            // Otherwise, remember if this was provisionally accepted for next partial parse
            LastResultProvisional = (result & PartialParseResult.Provisional) == PartialParseResult.Provisional;
            VerifyFlagsAreValid(result);

            return result;
        }

        /// <summary>
        /// Disposes of this parser. Should be called when the editor window is closed and the document is unloaded.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _parser.Dispose();
            }
        }

        private PartialParseResult TryPartialParse(TextChange change)
        {
            var result = PartialParseResult.Rejected;

            // Try the last change owner
            if (_lastChangeOwner != null && _lastChangeOwner.EditHandler.OwnsChange(_lastChangeOwner, change))
            {
                var editResult = _lastChangeOwner.EditHandler.ApplyChange(_lastChangeOwner, change);
                result = editResult.Result;
                if ((editResult.Result & PartialParseResult.Rejected) != PartialParseResult.Rejected)
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
                result = PartialParseResult.Rejected;
            }
            else if (_lastChangeOwner != null)
            {
                var editResult = _lastChangeOwner.EditHandler.ApplyChange(_lastChangeOwner, change);
                result = editResult.Result;
                if ((editResult.Result & PartialParseResult.Rejected) != PartialParseResult.Rejected)
                {
                    _lastChangeOwner.ReplaceWith(editResult.EditedSpan);
                }
                if ((result & PartialParseResult.AutoCompleteBlock) == PartialParseResult.AutoCompleteBlock)
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
                _currentSyntaxTree = args.GeneratorResults.GetSyntaxTree();
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
            Debug.Assert(((result & PartialParseResult.Accepted) == PartialParseResult.Accepted) ||
                         ((result & PartialParseResult.Rejected) == PartialParseResult.Rejected),
                         "Partial Parse result does not have either of Accepted or Rejected flags set");
            Debug.Assert(((result & PartialParseResult.Rejected) == PartialParseResult.Rejected) ||
                         ((result & PartialParseResult.SpanContextChanged) != PartialParseResult.SpanContextChanged),
                         "Partial Parse result was Accepted AND had SpanContextChanged flag set");
            Debug.Assert(((result & PartialParseResult.Rejected) == PartialParseResult.Rejected) ||
                         ((result & PartialParseResult.AutoCompleteBlock) != PartialParseResult.AutoCompleteBlock),
                         "Partial Parse result was Accepted AND had AutoCompleteBlock flag set");
            Debug.Assert(((result & PartialParseResult.Accepted) == PartialParseResult.Accepted) ||
                         ((result & PartialParseResult.Provisional) != PartialParseResult.Provisional),
                         "Partial Parse result was Rejected AND had Provisional flag set");
        }
    }
}
