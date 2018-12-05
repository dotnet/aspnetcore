// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class ClassifiedSpanRewriter : SyntaxRewriter
    {
        public override SyntaxNode VisitMarkupStartTag(MarkupStartTagSyntax node)
        {
            SpanContext latestSpanContext = null;
            var newChildren = SyntaxListBuilder<RazorSyntaxNode>.Create();
            var literals = new List<MarkupTextLiteralSyntax>();
            foreach (var child in node.Children)
            {
                if (child is MarkupTextLiteralSyntax literal)
                {
                    literals.Add(literal);
                    latestSpanContext = literal.GetSpanContext() ?? latestSpanContext;
                }
                else if (child is MarkupMiscAttributeContentSyntax miscContent)
                {
                    foreach (var contentChild in miscContent.Children)
                    {
                        if (contentChild is MarkupTextLiteralSyntax contentLiteral)
                        {
                            literals.Add(contentLiteral);
                            latestSpanContext = contentLiteral.GetSpanContext() ?? latestSpanContext;
                        }
                        else
                        {
                            // Pop stack
                            AddLiteralIfExists();
                            newChildren.Add(contentChild);
                        }
                    }
                }
                else
                {
                    AddLiteralIfExists();
                    newChildren.Add(child);
                }
            }

            AddLiteralIfExists();

            return SyntaxFactory.MarkupStartTag(newChildren.ToList()).Green.CreateRed(node.Parent, node.Position);

            void AddLiteralIfExists()
            {
                if (literals.Count > 0)
                {
                    var mergedLiteral = SyntaxUtilities.MergeTextLiterals(literals.ToArray());
                    mergedLiteral = mergedLiteral.WithSpanContext(latestSpanContext);
                    literals.Clear();
                    latestSpanContext = null;
                    newChildren.Add(mergedLiteral);
                }
            }
        }
    }
}
