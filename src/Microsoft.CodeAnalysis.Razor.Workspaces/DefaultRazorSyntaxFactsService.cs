// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Span = Microsoft.AspNetCore.Razor.Language.Legacy.Span;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultRazorSyntaxFactsService : RazorSyntaxFactsService
    {
        public override IReadOnlyList<ClassifiedSpan> GetClassifiedSpans(RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var spans = Flatten(syntaxTree);

            var result = new ClassifiedSpan[spans.Count];
            for (var i = 0; i < spans.Count; i++)
            {
                var span = spans[i];
                result[i] = new ClassifiedSpan(
                    new SourceSpan(
                        span.Start.FilePath ?? syntaxTree.Source.FilePath,
                        span.Start.AbsoluteIndex,
                        span.Start.LineIndex,
                        span.Start.CharacterIndex,
                        span.Length),
                    new SourceSpan(
                        span.Parent.Start.FilePath ?? syntaxTree.Source.FilePath,
                        span.Parent.Start.AbsoluteIndex,
                        span.Parent.Start.LineIndex,
                        span.Parent.Start.CharacterIndex,
                        span.Parent.Length),
                    (SpanKind)span.Kind,
                    (BlockKind)span.Parent.Type,
                    (AcceptedCharacters)span.EditHandler.AcceptedCharacters);
            }

            return result;
        }

        private List<Span> Flatten(RazorSyntaxTree syntaxTree)
        {
            var result = new List<Span>();
            AppendFlattenedSpans(syntaxTree.Root, result);
            return result;

            void AppendFlattenedSpans(SyntaxTreeNode node, List<Span> foundSpans)
            {
                Span spanNode = node as Span;
                if (spanNode != null)
                {
                    foundSpans.Add(spanNode);
                }
                else
                {
                    TagHelperBlock tagHelperNode = node as TagHelperBlock;
                    if (tagHelperNode != null)
                    {
                        // These aren't in document order, sort them first and then dig in
                        List<SyntaxTreeNode> attributeNodes = tagHelperNode.Attributes.Select(kvp => kvp.Value).Where(att => att != null).ToList();
                        attributeNodes.Sort((x, y) => x.Start.AbsoluteIndex.CompareTo(y.Start.AbsoluteIndex));

                        foreach (SyntaxTreeNode curNode in attributeNodes)
                        {
                            AppendFlattenedSpans(curNode, foundSpans);
                        }
                    }

                    Block blockNode = node as Block;
                    if (blockNode != null)
                    {
                        foreach (SyntaxTreeNode curNode in blockNode.Children)
                        {
                            AppendFlattenedSpans(curNode, foundSpans);
                        }
                    }
                }
            }
        }

        public override IReadOnlyList<TagHelperSpan> GetTagHelperSpans(RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var results = new List<TagHelperSpan>();

            List<Block> toProcess = new List<Block>();
            List<Block> blockChildren = new List<Block>();
            toProcess.Add(syntaxTree.Root);

            for (var i = 0; i < toProcess.Count; i++)
            {
                var blockNode = toProcess[i];
                TagHelperBlock tagHelperNode = blockNode as TagHelperBlock;
                if (tagHelperNode != null)
                {
                    results.Add(new TagHelperSpan(
                        new SourceSpan(
                            tagHelperNode.Start.FilePath ?? syntaxTree.Source.FilePath,
                            tagHelperNode.Start.AbsoluteIndex,
                            tagHelperNode.Start.LineIndex,
                            tagHelperNode.Start.CharacterIndex,
                            tagHelperNode.Length),
                        tagHelperNode.Binding));
                }

                // collect all child blocks and inject into toProcess as a single InsertRange
                foreach (SyntaxTreeNode curNode in blockNode.Children)
                {
                    Block curBlock = curNode as Block;
                    if (curBlock != null)
                    {
                        blockChildren.Add(curBlock);
                    }
                }

                if (blockChildren.Count > 0)
                {
                    toProcess.InsertRange(i + 1, blockChildren);
                    blockChildren.Clear();
                }
            }

            return results;
        }

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
