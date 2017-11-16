// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

            var previousLine = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
            var trackingPoint = previousLine.Snapshot.CreateTrackingPoint(previousLine.End, PointTrackingMode.Negative);
            var previousLineEndIndex = trackingPoint.GetPosition(syntaxTreeSnapshot);

            var simulatedChange = new SourceChange(previousLineEndIndex, 0, string.Empty);
            var owningSpan = LocateOwner(syntaxTree.Root, simulatedChange);
            if (owningSpan.Kind == SpanKindInternal.Code)
            {
                // Example,
                // @{\n
                //   ^  - The newline here is a code span and we should just let the default c# editor take care of indentation.

                return null;
            }

            int? desiredIndentation = null;

            SyntaxTreeNode owningChild = owningSpan;
            while ((owningChild.Parent != null) && !desiredIndentation.HasValue)
            {
                var owningParent = owningChild.Parent;
                var children = new List<SyntaxTreeNode>(owningParent.Children);
                for (var i = 0; i < children.Count; i++)
                {
                    var currentChild = children[i];
                    if (!currentChild.IsBlock)
                    {
                        var currentSpan = currentChild as Span;
                        if (currentSpan.Symbols.Count == 1 &&
                            currentSpan.Symbols[0] is CSharpSymbol symbol &&
                            symbol.Type == CSharpSymbolType.LeftBrace)
                        {
                            var extraIndent = 0;

                            // Dev11 337312: Only indent one level deeper if the item after the open curly brace is a markup block
                            if (i < children.Count - 1)
                            {
                                var nextChild = children[i + 1];
                                if (nextChild.IsBlock && ((nextChild as Block).Type == BlockKindInternal.Markup))
                                {
                                    extraIndent = indentSize;
                                }
                            }

                            // We can't rely on the syntax trees representation of the source document because partial parses may have mutated
                            // the underlying SyntaxTree text buffer. Because of this, if we want to provide accurate indentations we need to
                            // operate on the current line representation as indicated by the provider.
                            var lineText = line.Snapshot.GetLineFromLineNumber(currentSpan.Start.LineIndex).GetText();
                            desiredIndentation = GetIndentLevelOfLine(lineText, tabSize) + indentSize;
                        }
                    }

                    if (currentChild == owningChild)
                    {
                        break;
                    }
                }

                owningChild = owningParent;
            }

            return desiredIndentation;
        }

        private Span LocateOwner(Block root, SourceChange change)
        {
            // Ask each child recursively
            Span owner = null;
            foreach (var element in root.Children)
            {
                if (element.Start.AbsoluteIndex > change.Span.AbsoluteIndex)
                {
                    // too far
                    break;
                }

                var elementLen = element.Length;
                if (element.Start.AbsoluteIndex + elementLen < change.Span.AbsoluteIndex)
                {
                    // not far enough
                    continue;
                }

                if (element.IsBlock)
                {
                    var block = element as Block;

                    if (element.Start.AbsoluteIndex + elementLen == change.Span.AbsoluteIndex)
                    {
                        var lastDescendant = block.FindLastDescendentSpan();
                        if ((lastDescendant == null) && (block is TagHelperBlock))
                        {
                            var tagHelperBlock = (TagHelperBlock)block;
                            if (tagHelperBlock.SourceEndTag != null)
                            {
                                lastDescendant = tagHelperBlock.SourceEndTag.FindLastDescendentSpan();
                            }
                            else if (tagHelperBlock.SourceStartTag != null)
                            {
                                lastDescendant = tagHelperBlock.SourceStartTag.FindLastDescendentSpan();
                            }
                        }

                        // Conceptually, lastDescendant should always be non-null, but runtime errs on some
                        //   cases and makes empty blocks. Runtime will fix these issues as we find them, but make
                        //   no guarantee that they catch them all.
                        if (lastDescendant == null)
                        {
                            owner = LocateOwner(block, change);
                            if (owner != null)
                            {
                                break;
                            }
                        }
                        else if (lastDescendant.EditHandler.OwnsChange(lastDescendant, change))
                        {
                            owner = lastDescendant;
                            break;
                        }
                    }
                    else
                    {
                        owner = LocateOwner(block, change);
                        if (owner != null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    var span = element as Span;
                    if (span.EditHandler.OwnsChange(span, change))
                    {
                        owner = span;
                        break;
                    }
                }
            }

            if (owner == null)
            {
                if (root is TagHelperBlock tagHelperNode)
                {
                    var sourceStartTag = tagHelperNode.SourceStartTag;
                    var sourceEndTag = tagHelperNode.SourceEndTag;
                    if ((sourceStartTag.Start.AbsoluteIndex <= change.Span.AbsoluteIndex) &&
                        (sourceStartTag.Start.AbsoluteIndex + sourceStartTag.Length >= change.Span.AbsoluteIndex))
                    {
                        // intersects the start tag
                        return LocateOwner(sourceStartTag, change);
                    }
                    else if ((sourceEndTag.Start.AbsoluteIndex <= change.Span.AbsoluteIndex) &&
                                (sourceEndTag.Start.AbsoluteIndex + sourceEndTag.Length >= change.Span.AbsoluteIndex))
                    {
                        // intersects the end tag
                        return LocateOwner(sourceEndTag, change);
                    }
                }
            }

            return owner;
        }

        private int GetIndentLevelOfLine(string line, int tabSize)
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
    }
}
