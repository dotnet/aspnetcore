// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ModelExpressionPass : RazorIRPassBase, IRazorIROptimizationPass
    {
        private const string ModelExpressionTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression";

        public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
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

                    if (node.Children.Count == 1 && node.Children[0] is HtmlContentIRNode)
                    {
                        // A 'simple' expression will look like __model => __model.Foo
                        //
                        // Note that the fact we're looking for HTML here is based on a bug.
                        // https://github.com/aspnet/Razor/issues/963
                        var original = ((HtmlContentIRNode)node.Children[0]);

                        builder.Add(new RazorIRToken()
                        {
                            Kind = RazorIRToken.TokenKind.CSharp,
                            Content = "__model."
                        });

                        builder.Add(new RazorIRToken()
                        {
                            Kind = RazorIRToken.TokenKind.CSharp,
                            Content = original.Content,
                            Source = original.Source,
                        });
                    }
                    else
                    {
                        for (var i = 0; i < node.Children.Count; i++)
                        {
                            var nestedExpression = node.Children[i] as CSharpExpressionIRNode;
                            if (nestedExpression != null)
                            {
                                for (var j = 0; j < nestedExpression.Children.Count; j++)
                                {
                                    var cSharpToken = nestedExpression.Children[j] as RazorIRToken;
                                    if (cSharpToken != null && cSharpToken.Kind == RazorIRToken.TokenKind.CSharp)
                                    {
                                        builder.Add(cSharpToken);
                                    }
                                }

                                continue;
                            }

                            // Note that the fact we're looking for HTML here is based on a bug.
                            // https://github.com/aspnet/Razor/issues/963
                            var html = node.Children[i] as HtmlContentIRNode;
                            if (html != null)
                            {
                                builder.Add(new RazorIRToken()
                                {
                                    Kind = RazorIRToken.TokenKind.CSharp,
                                    Content = html.Content,
                                    Source = html.Source,
                                });
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
                    expression.Parent = node;
                }
            }
        }
    }
}
