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

                    if (node.Children.Count == 1 && node.Children[0] is HtmlContentIRNode original)
                    {
                        // A 'simple' expression will look like __model => __model.Foo
                        //
                        // Note that the fact we're looking for HTML here is based on a bug.
                        // https://github.com/aspnet/Razor/issues/963

                        builder.Add(new RazorIRToken()
                        {
                            Kind = RazorIRToken.TokenKind.CSharp,
                            Content = "__model."
                        });

                        var content = GetContent(original);
                        builder.Add(new RazorIRToken()
                        {
                            Kind = RazorIRToken.TokenKind.CSharp,
                            Content = content,
                            Source = original.Source,
                        });
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

                            // Note that the fact we're looking for HTML here is based on a bug.
                            // https://github.com/aspnet/Razor/issues/963
                            if (node.Children[i] is HtmlContentIRNode html)
                            {
                                var content = GetContent(html);
                                builder.Add(new RazorIRToken()
                                {
                                    Kind = RazorIRToken.TokenKind.CSharp,
                                    Content = content,
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

            private string GetContent(HtmlContentIRNode node)
            {
                var builder = new StringBuilder();
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is RazorIRToken token && token.IsHtml)
                    {
                        builder.Append(token.Content);
                    }
                }

                return builder.ToString();
            }
        }
    }
}
