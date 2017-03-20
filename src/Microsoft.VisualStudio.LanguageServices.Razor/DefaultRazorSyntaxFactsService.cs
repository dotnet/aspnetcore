// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(RazorSyntaxFactsService))]
    internal class DefaultRazorSyntaxFactsService : RazorSyntaxFactsService
    {
        public override IReadOnlyList<ClassifiedSpan> GetClassifiedSpans(RazorSyntaxTree syntaxTree)
        {
            var spans = Flatten(syntaxTree);

            var result = new ClassifiedSpan[spans.Count];
            for (var i = 0; i < spans.Count; i++)
            {
                var span = spans[i];
                result[i] = new ClassifiedSpan(
                    new SourceSpan(
                        span.Start.FilePath ?? syntaxTree.Source.FileName,
                        span.Start.AbsoluteIndex,
                        span.Start.LineIndex,
                        span.Start.CharacterIndex,
                        span.Length),
                    new SourceSpan(
                        span.Parent.Start.FilePath ?? syntaxTree.Source.FileName,
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
                        attributeNodes.Sort((x, y) => x.Start.AbsoluteIndex.CompareTo(y.Start));

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
    }
}
