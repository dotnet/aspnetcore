// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ModelExpressionPass : RazorIRPassBase, IRazorIROptimizationPass
    {
        private const string ModelExpressionTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression";

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var visitor = new Visitor();
            visitor.Visit(irDocument);
        }

        private class Visitor : RazorIRNodeWalker
        {
            public List<TagHelperIRNode> TagHelpers { get; } = new List<TagHelperIRNode>();

            public override void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
            {
                if (string.Equals(node.Descriptor.TypeName, ModelExpressionTypeName, StringComparison.Ordinal) ||
                    (node.IsIndexerNameMatch &&
                     string.Equals(node.Descriptor.IndexerTypeName, ModelExpressionTypeName, StringComparison.Ordinal)))
                {
                    var expression = new CSharpExpressionIRNode();
                    var builder = RazorIRBuilder.Create(expression);

                    builder.Add(new RazorIRToken()
                    {
                        Kind = RazorIRToken.TokenKind.CSharp,
                        Content = "ModelExpressionProvider.CreateModelExpression(ViewData, __model => ",
                    });

                    if (node.Children.Count == 1 && node.Children[0] is RazorIRToken token && token.IsCSharp)
                    {
                        // A 'simple' expression will look like __model => __model.Foo

                        builder.Add(new RazorIRToken()
                        {
                            Kind = RazorIRToken.TokenKind.CSharp,
                            Content = "__model."
                        });

                        builder.Add(token);
                    }
                    else
                    {
                        for (var i = 0; i < node.Children.Count; i++)
                        {
                            if (node.Children[i] is CSharpExpressionIRNode nestedExpression)
                            {
                                for (var j = 0; j < nestedExpression.Children.Count; j++)
                                {
                                    if (nestedExpression.Children[j] is RazorIRToken cSharpToken &&
                                        cSharpToken.IsCSharp)
                                    {
                                        builder.Add(cSharpToken);
                                    }
                                }

                                continue;
                            }
                        }
                    }

                    builder.Add(new RazorIRToken()
                    {
                        Kind = RazorIRToken.TokenKind.CSharp,
                        Content = ")",
                    });

                    node.Children.Clear();

                    node.Children.Add(expression);
                }
            }
        }
    }
}
