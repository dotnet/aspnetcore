// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class EventHandlerLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            // For each event handler *usage* we need to rewrite the tag helper node to map to basic constructs.
            var nodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                ProcessDuplicates(node);

                for (var j = node.Children.Count - 1; j >= 0; j--)
                {
                    var attributeNode = node.Children[j] as ComponentAttributeExtensionNode;
                    if (attributeNode != null &&
                        attributeNode.TagHelper != null &&
                        attributeNode.TagHelper.IsEventHandlerTagHelper())
                    {
                        RewriteUsage(node, j, attributeNode);
                    }
                }
            }
        }

        private void ProcessDuplicates(TagHelperIntermediateNode node)
        {
            // Reverse order because we will remove nodes.
            //
            // Each 'property' node could be duplicated if there are multiple tag helpers that match that
            // particular attribute. This is likely to happen when a component also defines something like
            // OnClick. We want to remove the 'onclick' and let it fall back to be handled by the component.
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                var attributeNode = node.Children[i] as ComponentAttributeExtensionNode;
                if (attributeNode != null &&
                    attributeNode.TagHelper != null &&
                    attributeNode.TagHelper.IsEventHandlerTagHelper())
                {
                    for (var j = 0; j < node.Children.Count; j++)
                    {
                        var duplicate = node.Children[j] as ComponentAttributeExtensionNode;
                        if (duplicate != null &&
                            duplicate.TagHelper != null &&
                            duplicate.TagHelper.IsComponentTagHelper() &&
                            duplicate.AttributeName == attributeNode.AttributeName)
                        {
                            // Found a duplicate - remove the 'fallback' in favor of the
                            // more specific tag helper.
                            node.Children.RemoveAt(i);
                            node.TagHelpers.Remove(attributeNode.TagHelper);
                            break;
                        }
                    }
                }
            }

            // If we still have duplicates at this point then they are genuine conflicts.
            var duplicates = node.Children
                .OfType<ComponentAttributeExtensionNode>()
                .Where(p => p.TagHelper?.IsEventHandlerTagHelper() ?? false)
                .GroupBy(p => p.AttributeName)
                .Where(g => g.Count() > 1);

            foreach (var duplicate in duplicates)
            {
                node.Diagnostics.Add(BlazorDiagnosticFactory.CreateEventHandler_Duplicates(
                    node.Source,
                    duplicate.Key,
                    duplicate.ToArray()));
                foreach (var property in duplicate)
                {
                    node.Children.Remove(property);
                }
            }
        }

        private void RewriteUsage(TagHelperIntermediateNode node, int index, ComponentAttributeExtensionNode attributeNode)
        {
            var original = GetAttributeContent(attributeNode);
            if (string.IsNullOrEmpty(original.Content))
            {
                // This can happen in error cases, the parser will already have flagged this
                // as an error, so ignore it.
                return;
            }

            var rewrittenNode = new ComponentAttributeExtensionNode(attributeNode);
            node.Children[index] = rewrittenNode;

            // Now rewrite the content of the value node to look like:
            //
            // BindMethods.GetEventHandlerValue<TDelegate>(<code>)
            //
            // This method is overloaded on string and TDelegate, which means that it will put the code in the
            // correct context for intellisense when typing in the attribute.
            var eventArgsType = attributeNode.TagHelper.GetEventArgsType();

            rewrittenNode.Children.Clear();
            rewrittenNode.Children.Add(new CSharpExpressionIntermediateNode()
            {
                Children =
                {
                    new IntermediateToken()
                    {
                        Content = $"{BlazorApi.BindMethods.GetEventHandlerValue}<{eventArgsType}>(",
                        Kind = TokenKind.CSharp
                    },
                    original,
                    new IntermediateToken()
                    {
                        Content = $")",
                        Kind = TokenKind.CSharp
                    }
                },
            });
        }

        private static IntermediateToken GetAttributeContent(ComponentAttributeExtensionNode node)
        {
            if (node.Children[0] is HtmlContentIntermediateNode htmlContentNode)
            {
                // This case can be hit for a 'string' attribute. We want to turn it into
                // an expression.
                var content = "\"" + ((IntermediateToken)htmlContentNode.Children.Single()).Content + "\"";
                return new IntermediateToken() { Content = content, Kind = TokenKind.CSharp, };
            }
            else if (node.Children[0] is CSharpExpressionIntermediateNode cSharpNode)
            {
                // This case can be hit when the attribute has an explicit @ inside, which
                // 'escapes' any special sugar we provide for codegen.
                return ((IntermediateToken)cSharpNode.Children.Single());
            }
            else
            {
                // This is the common case for 'mixed' content
                return ((IntermediateToken)node.Children.Single());
            }
        }
    }
}
