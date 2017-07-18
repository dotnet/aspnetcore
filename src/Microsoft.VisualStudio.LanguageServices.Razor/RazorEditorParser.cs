// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
    }
}
