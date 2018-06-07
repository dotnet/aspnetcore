// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using ITextBuffer = Microsoft.VisualStudio.Text.ITextBuffer;

namespace Microsoft.VisualStudio.Editor.Razor
{
    /// <summary>
    /// This class is responsible for handling situations where Roslyn and the HTML editor cannot auto-indent Razor code.
    /// </summary>
    /// <example>
    /// Attempting to insert a newline (pipe indicates the cursor):
    /// @{ |}
    /// Should result in the text buffer looking like the following:
    /// @{
    ///     |
    /// }
    /// This is also true for directive block scenarios.
    /// </example>
    internal class BraceSmartIndenter : IDisposable
    {
        private readonly ForegroundDispatcher _dispatcher;
        private readonly ITextBuffer _textBuffer;
        private readonly VisualStudioDocumentTracker _documentTracker;
        private readonly TextBufferCodeDocumentProvider _codeDocumentProvider;
        private readonly IEditorOperationsFactoryService _editorOperationsFactory;
        private readonly StringBuilder _indentBuilder = new StringBuilder();
        private BraceIndentationContext _context;

        // Internal for testing
        internal BraceSmartIndenter()
        {
        }

        public BraceSmartIndenter(
            ForegroundDispatcher dispatcher,
            VisualStudioDocumentTracker documentTracker,
            TextBufferCodeDocumentProvider codeDocumentProvider,
            IEditorOperationsFactoryService editorOperationsFactory)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            if (documentTracker == null)
            {
                throw new ArgumentNullException(nameof(documentTracker));
            }

            if (codeDocumentProvider == null)
            {
                throw new ArgumentNullException(nameof(codeDocumentProvider));
            }

            if (editorOperationsFactory == null)
            {
                throw new ArgumentNullException(nameof(editorOperationsFactory));
            }

            _dispatcher = dispatcher;
            _documentTracker = documentTracker;
            _codeDocumentProvider = codeDocumentProvider;
            _editorOperationsFactory = editorOperationsFactory;
            _textBuffer = _documentTracker.TextBuffer;
            _textBuffer.Changed += TextBuffer_OnChanged;
            _textBuffer.PostChanged += TextBuffer_OnPostChanged;
        }

        public void Dispose()
        {
            _dispatcher.AssertForegroundThread();

            _textBuffer.Changed -= TextBuffer_OnChanged;
            _textBuffer.PostChanged -= TextBuffer_OnPostChanged;
        }

        // Internal for testing
        internal void TriggerSmartIndent(ITextView textView)
        {
            // This forces the smart indent. For example attempting to enter a newline between the functions directive:
            // @functions {} will not auto-indent in between the braces unless we forcefully move to end of line.
            var editorOperations = _editorOperationsFactory.GetEditorOperations(textView);
            editorOperations.MoveToEndOfLine(false);
        }

        // Internal for testing
        internal void TextBuffer_OnChanged(object sender, TextContentChangedEventArgs args)
        {
            _dispatcher.AssertForegroundThread();

            if (!args.TextChangeOccurred(out var changeInformation))
            {
                return;
            }

            var newText = changeInformation.newText;
            if (!_codeDocumentProvider.TryGetFromBuffer(_documentTracker.TextBuffer, out var codeDocument))
            {
                // Parse not available.
                return;
            }

            var syntaxTree = codeDocument.GetSyntaxTree();
            if (TryCreateIndentationContext(changeInformation.firstChange.NewPosition, newText.Length, newText, syntaxTree, _documentTracker, out var context))
            {
                _context = context;
            }
        }

        private void TextBuffer_OnPostChanged(object sender, EventArgs e)
        {
            _dispatcher.AssertForegroundThread();

            var context = _context;
            _context = null;

            if (context != null)
            {
                // Save the current caret position
                var textView = context.FocusedTextView;
                var caret = textView.Caret.Position.BufferPosition;
                var textViewBuffer = textView.TextBuffer;
                var indent = CalculateIndent(textViewBuffer, context.ChangePosition);

                // Current state, pipe is cursor:
                // @{
                // |}

                // Insert the completion text, i.e. "\r\n      "
                InsertIndent(caret.Position, indent, textViewBuffer);

                // @{
                // 
                // |}

                // Place the caret inbetween the braces (before our indent).
                RestoreCaretTo(caret.Position, textView);

                // @{
                // |
                // }

                // For Razor metacode cases the editor's smart indent wont kick in automatically. 
                TriggerSmartIndent(textView);

                // @{
                //     |
                // }
            }
        }

        private string CalculateIndent(ITextBuffer buffer, int from)
        {
            // Get the line text of the block start
            var currentSnapshotPoint = new SnapshotPoint(buffer.CurrentSnapshot, from);
            var line = buffer.CurrentSnapshot.GetLineFromPosition(currentSnapshotPoint);
            var lineText = line.GetText();

            // Gather up the indent from the start block
            _indentBuilder.Append(line.GetLineBreakText());
            foreach (var ch in lineText)
            {
                if (!char.IsWhiteSpace(ch))
                {
                    break;
                }
                _indentBuilder.Append(ch);
            }

            var indent = _indentBuilder.ToString();
            _indentBuilder.Clear();

            return indent;
        }

        // Internal for testing
        internal static void InsertIndent(int insertLocation, string indent, ITextBuffer textBuffer)
        {
            var edit = textBuffer.CreateEdit();
            edit.Insert(insertLocation, indent);
            edit.Apply();
        }

        // Internal for testing
        internal static void RestoreCaretTo(int caretPosition, ITextView textView)
        {
            var currentSnapshotPoint = new SnapshotPoint(textView.TextBuffer.CurrentSnapshot, caretPosition);
            textView.Caret.MoveTo(currentSnapshotPoint);
        }

        // Internal for testing
        internal static bool TryCreateIndentationContext(
            int changePosition,
            int changeLength,
            string finalText,
            RazorSyntaxTree syntaxTree,
            VisualStudioDocumentTracker documentTracker,
            out BraceIndentationContext context)
        {
            var focusedTextView = documentTracker.GetFocusedTextView();
            if (focusedTextView != null && ParserHelpers.IsNewLine(finalText))
            {
                if (!AtValidContentKind(changePosition, syntaxTree))
                {
                    context = null;
                    return false;
                }

                var currentSnapshot = documentTracker.TextBuffer.CurrentSnapshot;
                var preChangeLineSnapshot = currentSnapshot.GetLineFromPosition(changePosition);

                // Handle the case where the \n comes through separately from the \r and the position
                // on the line is beyond what the GetText call above gives back.
                var linePosition = Math.Min(preChangeLineSnapshot.Length, changePosition - preChangeLineSnapshot.Start) - 1;

                if (AfterOpeningBrace(linePosition, preChangeLineSnapshot))
                {
                    var afterChangePosition = changePosition + changeLength;
                    var afterChangeLineSnapshot = currentSnapshot.GetLineFromPosition(afterChangePosition);
                    var afterChangeLinePosition = afterChangePosition - afterChangeLineSnapshot.Start;

                    if (BeforeClosingBrace(afterChangeLinePosition, afterChangeLineSnapshot))
                    {
                        context = new BraceIndentationContext(focusedTextView, changePosition);
                        return true;
                    }
                }
            }

            context = null;
            return false;
        }

        // Internal for testing
        internal static bool AtValidContentKind(int changePosition, RazorSyntaxTree syntaxTree)
        {
            var change = new SourceChange(changePosition, 0, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                return false;
            }

            if (owner.Kind == SpanKindInternal.MetaCode)
            {
                // @functions{|}
                return true;
            }

            if (owner.Kind == SpanKindInternal.Code)
            {
                // It's important that we still indent in C# cases because in the example below we're asked for
                // a content validation kind check at a 0 length C# code Span (marker).
                // In the case that we do a smart indent in a situation when Roslyn would: Roslyn respects our
                // indentation attempt and applies any additional modifications to the applicable span.
                // @{|}
                return true;
            }
            
            return false;
        }

        internal static bool BeforeClosingBrace(int linePosition, ITextSnapshotLine lineSnapshot)
        {
            var lineText = lineSnapshot.GetText();
            for (; linePosition < lineSnapshot.Length; linePosition++)
            {
                if (!char.IsWhiteSpace(lineText[linePosition]))
                {
                    break;
                }
            }

            var beforeClosingBrace = linePosition < lineSnapshot.Length && lineText[linePosition] == '}';
            return beforeClosingBrace;
        }

        internal static bool AfterOpeningBrace(int linePosition, ITextSnapshotLine lineSnapshot)
        {
            var lineText = lineSnapshot.GetText();
            for (; linePosition >= 0; linePosition--)
            {
                if (!char.IsWhiteSpace(lineText[linePosition]))
                {
                    break;
                }
            }

            var afterClosingBrace = linePosition >= 0 && lineText[linePosition] == '{';
            return afterClosingBrace;
        }

        internal class BraceIndentationContext
        {
            public BraceIndentationContext(ITextView focusedTextView, int changePosition)
            {
                FocusedTextView = focusedTextView;
                ChangePosition = changePosition;
            }

            public ITextView FocusedTextView { get; }

            public int ChangePosition { get; }
        }
    }
}
