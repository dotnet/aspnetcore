// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Span = Microsoft.AspNetCore.Razor.Language.Legacy.Span;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(RazorSyntaxFactsService))]
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
    }
}
