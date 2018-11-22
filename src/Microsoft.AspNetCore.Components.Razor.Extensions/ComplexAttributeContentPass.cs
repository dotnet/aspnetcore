// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    // We don't support 'complex' content for components (mixed C# and markup) right now.
    // It's not clear yet if Blazor will have a good scenario to use these constructs.
    //
    // This is where a lot of the complexity in the Razor/TagHelpers model creeps in and we
    // might be able to avoid it if these features aren't needed.
    internal class ComplexAttributeContentPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run before other Blazor passes
        public override int Order => -1000;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var nodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                ProcessAttributes(nodes[i]);
            }
        }

        private void ProcessAttributes(TagHelperIntermediateNode node)
        {
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                if (node.Children[i] is TagHelperPropertyIntermediateNode propertyNode)
                {
                    if (TrySimplifyContent(propertyNode) && node.TagHelpers.Any(t => t.IsComponentTagHelper()))
                    {
                        node.Diagnostics.Add(BlazorDiagnosticFactory.Create_UnsupportedComplexContent(
                            propertyNode,
                            propertyNode.AttributeName));
                        node.Children.RemoveAt(i);
                        continue;
                    }
                }
                else if (node.Children[i] is TagHelperHtmlAttributeIntermediateNode htmlNode)
                {
                    if (TrySimplifyContent(htmlNode) && node.TagHelpers.Any(t => t.IsComponentTagHelper()))
                    {
                        node.Diagnostics.Add(BlazorDiagnosticFactory.Create_UnsupportedComplexContent(
                            htmlNode,
                            htmlNode.AttributeName));
                        node.Children.RemoveAt(i);
                        continue;
                    }
                }
            }
        }

        private static bool TrySimplifyContent(IntermediateNode node)
        {
            if (node.Children.Count == 1 &&
                node.Children[0] is HtmlAttributeIntermediateNode htmlNode &&
                htmlNode.Children.Count > 1)
            {
                // This case can be hit for a 'string' attribute
                return true;
            }
            else if (node.Children.Count == 1 &&
                node.Children[0] is CSharpExpressionIntermediateNode cSharpNode &&
                cSharpNode.Children.Count > 1)
            {
                // This case can be hit when the attribute has an explicit @ inside, which
                // 'escapes' any special sugar we provide for codegen.
                //
                // There's a special case here for explicit expressions. See https://github.com/aspnet/Razor/issues/2203
                // handling this case as a tactical matter since it's important for lambdas.
                if (cSharpNode.Children.Count == 3 &&
                    cSharpNode.Children[0] is IntermediateToken token0 &&
                    cSharpNode.Children[2] is IntermediateToken token2 &&
                    token0.Content == "(" &&
                    token2.Content == ")")
                {
                    cSharpNode.Children.RemoveAt(2);
                    cSharpNode.Children.RemoveAt(0);

                    // We were able to simplify it, all good.
                    return false;
                }

                return true;
            }
            else if (node.Children.Count == 1 &&
                node.Children[0] is CSharpCodeIntermediateNode cSharpCodeNode)
            {
                // This is the case when an attribute contains a code block @{ ... }
                // We don't support this.
                return true;
            }
            else if (node.Children.Count > 1)
            {
                // This is the common case for 'mixed' content
                return true;
            }

            return false;
        }
    }
}
