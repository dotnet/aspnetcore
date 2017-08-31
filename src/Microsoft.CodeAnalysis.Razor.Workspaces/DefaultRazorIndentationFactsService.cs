// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultRazorIndentationFactsService : RazorIndentationFactsService
    {
        public override int? GetDesiredIndentation(RazorSyntaxTree syntaxTree, int previousLineEndIndex, Func<int, string> getLineContent, int indentSize, int tabSize)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            if (previousLineEndIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(previousLineEndIndex));
            }

            if (getLineContent == null)
            {
                throw new ArgumentNullException(nameof(getLineContent));
            }

            if (indentSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(indentSize));
            }

            if (tabSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tabSize));
            }

            var simulatedChange = new SourceChange(previousLineEndIndex, 0, string.Empty);
            var owningSpan = LocateOwner(syntaxTree.Root, simulatedChange);

            int? desiredIndentation = null;

            if (owningSpan.Kind != SpanKindInternal.Code)
            {
                SyntaxTreeNode owningChild = owningSpan;
                while ((owningChild.Parent != null) && !desiredIndentation.HasValue)
                {
                    Block owningParent = owningChild.Parent;
                    List<SyntaxTreeNode> children = new List<SyntaxTreeNode>(owningParent.Children);
                    for (int i = 0; i < children.Count; i++)
                    {
                        SyntaxTreeNode curChild = children[i];
                        if (!curChild.IsBlock)
                        {
                            Span curSpan = curChild as Span;
                            if (curSpan.Kind == SpanKindInternal.MetaCode)
                            {
                                var extraIndent = 0;

                                // Dev11 337312: Only indent one level deeper if the item after the metacode is a markup block
                                if (i < children.Count - 1)
                                {
                                    SyntaxTreeNode nextChild = children[i + 1];
                                    if (nextChild.IsBlock && ((nextChild as Block).Type == BlockKindInternal.Markup))
                                    {
                                        extraIndent = indentSize;
                                    }
                                }

                                // We can't rely on the syntax trees representation of the source document because partial parses may have mutated
                                // the underlying SyntaxTree text buffer. Because of this, if we want to provide accurate indentations we need to
                                // operate on the current line representation as indicated by the provider.
                                var line = getLineContent(curSpan.Start.LineIndex);
                                desiredIndentation = GetIndentLevelOfLine(line, tabSize) + indentSize;
                            }
                        }

                        if (curChild == owningChild)
                        {
                            break;
                        }
                    }

                    owningChild = owningParent;
                }
            }

            return desiredIndentation;
        }

        private Span LocateOwner(Block root, SourceChange change)
        {
            // Ask each child recursively
            Span owner = null;
            foreach (SyntaxTreeNode element in root.Children)
            {
                if (element.Start.AbsoluteIndex > change.Span.AbsoluteIndex)
                {
                    // too far
                    break;
                }

                int elementLen = element.Length;
                if (element.Start.AbsoluteIndex + elementLen < change.Span.AbsoluteIndex)
                {
                    // not far enough
                    continue;
                }

                if (element.IsBlock)
                {
                    Block block = element as Block;

                    if (element.Start.AbsoluteIndex + elementLen == change.Span.AbsoluteIndex)
                    {
                        Span lastDescendant = block.FindLastDescendentSpan();
                        if ((lastDescendant == null) && (block is TagHelperBlock))
                        {
                            TagHelperBlock tagHelperBlock = (TagHelperBlock)block;
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
                    Span span = element as Span;
                    if (span.EditHandler.OwnsChange(span, change))
                    {
                        owner = span;
                        break;
                    }
                }
            }

            if (owner == null)
            {
                TagHelperBlock tagHelperNode = root as TagHelperBlock;
                if (tagHelperNode != null)
                {
                    Block sourceStartTag = tagHelperNode.SourceStartTag;
                    Block sourceEndTag = tagHelperNode.SourceEndTag;
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
