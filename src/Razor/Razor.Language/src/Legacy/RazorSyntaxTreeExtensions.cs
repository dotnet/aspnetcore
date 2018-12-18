// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal static class RazorSyntaxTreeExtensions
    {
        public static IReadOnlyList<ClassifiedSpanInternal> GetClassifiedSpans(this RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var spans = Flatten(syntaxTree);

            var result = new ClassifiedSpanInternal[spans.Count];
            for (var i = 0; i < spans.Count; i++)
            {
                var span = spans[i];
                result[i] = new ClassifiedSpanInternal(
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
                    span.Kind,
                    span.Parent.Type,
                    span.EditHandler.AcceptedCharacters);
            }

            return result;
        }

        public static IReadOnlyList<TagHelperSpanInternal> GetTagHelperSpans(this RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var results = new List<TagHelperSpanInternal>();

            var toProcess = new List<Block>();
            var blockChildren = new List<Block>();
            toProcess.Add(syntaxTree.Root);

            for (var i = 0; i < toProcess.Count; i++)
            {
                var blockNode = toProcess[i];
                if (blockNode is TagHelperBlock tagHelperNode)
                {
                    results.Add(new TagHelperSpanInternal(
                        new SourceSpan(
                            tagHelperNode.Start.FilePath ?? syntaxTree.Source.FilePath,
                            tagHelperNode.Start.AbsoluteIndex,
                            tagHelperNode.Start.LineIndex,
                            tagHelperNode.Start.CharacterIndex,
                            tagHelperNode.Length),
                        tagHelperNode.Binding));
                }

                // collect all child blocks and inject into toProcess as a single InsertRange
                foreach (var child in blockNode.Children)
                {
                    if (child is Block block)
                    {
                        blockChildren.Add(block);
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

        private static List<Span> Flatten(RazorSyntaxTree syntaxTree)
        {
            var result = new List<Span>();
            AppendFlattenedSpans(syntaxTree.Root, result);
            return result;

            void AppendFlattenedSpans(SyntaxTreeNode node, List<Span> foundSpans)
            {
                if (node is Span spanNode)
                {
                    foundSpans.Add(spanNode);
                }
                else
                {
                    if (node is TagHelperBlock tagHelperNode)
                    {
                        // These aren't in document order, sort them first and then dig in
                        var attributeNodes = tagHelperNode.Attributes.Select(kvp => kvp.Value).Where(att => att != null).ToList();
                        attributeNodes.Sort((x, y) => x.Start.AbsoluteIndex.CompareTo(y.Start.AbsoluteIndex));

                        foreach (var attributeNode in attributeNodes)
                        {
                            AppendFlattenedSpans(attributeNode, foundSpans);
                        }
                    }

                    if (node is Block block)
                    {
                        foreach (var child in block.Children)
                        {
                            AppendFlattenedSpans(child, foundSpans);
                        }
                    }
                }
            }
        }
    }
}
