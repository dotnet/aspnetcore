// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.VisualStudio.Text;
using Span = Microsoft.AspNetCore.Razor.Language.Legacy.Span;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(RazorIndentationFactsService))]
    internal class DefaultRazorIndentationFactsService : RazorIndentationFactsService
    {
        // This method dives down a syntax tree looking for open curly braces, every time
        // it finds one it increments its indent until it finds the provided "line". 
        // 
        // Examples:
        // @{
        //    <strong>Hello World</strong>
        // }
        // Asking for desired indentation of the @{ or } lines should result in a desired indentation of 4.
        //
        // <div>
        //     @{
        //         <strong>Hello World</strong>
        //     }
        // </div>
        // Asking for desired indentation of the @{ or } lines should result in a desired indentation of 8.
        public override int? GetDesiredIndentation(
            RazorSyntaxTree syntaxTree,
            ITextSnapshot syntaxTreeSnapshot,
            ITextSnapshotLine line,
            int indentSize,
            int tabSize)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            if (syntaxTreeSnapshot == null)
            {
                throw new ArgumentNullException(nameof(syntaxTreeSnapshot));
            }

            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (indentSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(indentSize));
            }

            if (tabSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tabSize));
            }

            var previousLineEndIndex = GetPreviousLineEndIndex(syntaxTreeSnapshot, line);
            var simulatedChange = new SourceChange(previousLineEndIndex, 0, string.Empty);
            var owningSpan = syntaxTree.Root.LocateOwner(simulatedChange);
            if (owningSpan == null || owningSpan.Kind == SpanKindInternal.Code)
            {
                // Example,
                // @{\n
                //   ^  - The newline here is a code span and we should just let the default c# editor take care of indentation.

                return null;
            }

            int? desiredIndentation = null;
            SyntaxTreeNode owningChild = owningSpan;
            while (owningChild.Parent != null)
            {
                var owningParent = owningChild.Parent;
                for (var i = 0; i < owningParent.Children.Count; i++)
                {
                    var currentChild = owningParent.Children[i];
                    if (IsCSharpOpenCurlyBrace(currentChild))
                    {
                        var lineText = line.Snapshot.GetLineFromLineNumber(currentChild.Start.LineIndex).GetText();
                        desiredIndentation = GetIndentLevelOfLine(lineText, tabSize) + indentSize;
                    }

                    if (currentChild == owningChild)
                    {
                        break;
                    }
                }

                if (desiredIndentation.HasValue)
                {
                    return desiredIndentation;
                }

                owningChild = owningParent;
            }

            // Couldn't determine indentation
            return null;
        }

        // Internal for testing
        internal int GetIndentLevelOfLine(string line, int tabSize)
        {
            var indentLevel = 0;

            foreach (var c in line)
            {
                if (!char.IsWhiteSpace(c))
                {
                    break;
                }
                else if (c == '\t')
                {
                    indentLevel += tabSize;
                }
                else
                {
                    indentLevel++;
                }
            }

            return indentLevel;
        }

        // Internal for testing
        internal static int GetPreviousLineEndIndex(ITextSnapshot syntaxTreeSnapshot, ITextSnapshotLine line)
        {
            var previousLine = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
            var trackingPoint = previousLine.Snapshot.CreateTrackingPoint(previousLine.End, PointTrackingMode.Negative);
            var previousLineEndIndex = trackingPoint.GetPosition(syntaxTreeSnapshot);
            return previousLineEndIndex;
        }

        // Internal for testing
        internal static bool IsCSharpOpenCurlyBrace(SyntaxTreeNode currentChild)
        {
            return currentChild is Span currentSpan &&
                currentSpan.Tokens.Count == 1 &&
                currentSpan.Tokens[0].Kind == SyntaxKind.LeftBrace;
        }
    }
}
