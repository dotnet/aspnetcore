// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
{
    public class ModelExpressionPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        private const string ModelExpressionTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression";

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var visitor = new Visitor();
            visitor.Visit(documentNode);
        }

        private class Visitor : IntermediateNodeWalker
        {
            public List<TagHelperIntermediateNode> TagHelpers { get; } = new List<TagHelperIntermediateNode>();

            public override void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node)
            {
                if (string.Equals(node.BoundAttribute.TypeName, ModelExpressionTypeName, StringComparison.Ordinal) ||
                    (node.IsIndexerNameMatch &&
                     string.Equals(node.BoundAttribute.IndexerTypeName, ModelExpressionTypeName, StringComparison.Ordinal)))
                {
                    var expression = new CSharpExpressionIntermediateNode();

                    expression.Children.Add(new IntermediateToken()
                    {
                        Kind = TokenKind.CSharp,
                        Content = "ModelExpressionProvider.CreateModelExpression(ViewData, __model => ",
                    });

                    if (node.Children.Count == 1 && node.Children[0] is IntermediateToken token && token.IsCSharp)
                    {
                        // A 'simple' expression will look like __model => __model.Foo

                        expression.Children.Add(new IntermediateToken()
                        {
                            Kind = TokenKind.CSharp,
                            Content = "__model."
                        });

                        expression.Children.Add(token);
                    }
                    else
                    {
                        for (var i = 0; i < node.Children.Count; i++)
                        {
                            if (node.Children[i] is CSharpExpressionIntermediateNode nestedExpression)
                            {
                                for (var j = 0; j < nestedExpression.Children.Count; j++)
                                {
                                    if (nestedExpression.Children[j] is IntermediateToken cSharpToken &&
                                        cSharpToken.IsCSharp)
                                    {
                                        expression.Children.Add(cSharpToken);
                                    }
                                }

                                continue;
                            }
                        }
                    }

                    expression.Children.Add(new IntermediateToken()
                    {
                        Kind = TokenKind.CSharp,
                        Content = ")",
                    });

                    node.Children.Clear();

                    node.Children.Add(expression);
                }
            }
        }
    }
}
