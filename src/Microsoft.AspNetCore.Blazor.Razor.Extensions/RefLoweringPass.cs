// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class RefLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after our other passes
        public override int Order => 1000;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            var nodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                for (var j = node.Children.Count - 1; j >= 0; j--)
                {
                    var attributeNode = node.Children[j] as ComponentAttributeExtensionNode;
                    if (attributeNode != null &&
                        attributeNode.TagHelper != null &&
                        attributeNode.TagHelper.IsRefTagHelper())
                    {
                        RewriteUsage(@class, node, j, attributeNode);
                    }
                }
            }
        }

        private void RewriteUsage(ClassDeclarationIntermediateNode classNode, TagHelperIntermediateNode node, int index, ComponentAttributeExtensionNode attributeNode)
        {
            // If we can't get a nonempty attribute name, do nothing because there will
            // already be a diagnostic for empty values
            var identifierToken = DetermineIdentifierToken(attributeNode);
            if (identifierToken != null)
            {
                node.Children.Remove(attributeNode);

                // Determine whether this is an element capture or a component capture, and
                // if applicable the type name that will appear in the resulting capture code
                var componentTagHelper = node.TagHelpers.FirstOrDefault(x => x.IsComponentTagHelper());
                if (componentTagHelper != null)
                {
                    // For components, the RefExtensionNode must go after all ComponentAttributeExtensionNode
                    // and ComponentBodyExtensionNode siblings because they translate to AddAttribute calls.
                    // We can therefore put it immediately before the ComponentCloseExtensionNode.
                    var componentCloseNodePosition = LastIndexOf(node.Children, n => n is ComponentCloseExtensionNode);
                    if (componentCloseNodePosition < 0)
                    {
                        // Should never happen - would imply we're running the lowering passes in the wrong order
                        throw new InvalidOperationException($"Cannot find {nameof(ComponentCloseExtensionNode)} among ref node siblings.");
                    }
                    
                    var refExtensionNode = new RefExtensionNode(identifierToken, componentTagHelper.GetTypeName());
                    node.Children.Insert(componentCloseNodePosition, refExtensionNode);
                }
                else
                {
                    // For elements, it doesn't matter how the RefExtensionNode is positioned
                    // among the children, as the node writer takes care of emitting the
                    // code at the right point after the AddAttribute calls
                    node.Children.Add(new RefExtensionNode(identifierToken));
                }
            }
        }

        private IntermediateToken DetermineIdentifierToken(ComponentAttributeExtensionNode attributeNode)
        {
            IntermediateToken foundToken = null;

            if (attributeNode.Children.Count == 1)
            {
                if (attributeNode.Children[0] is IntermediateToken token)
                {
                    foundToken = token;
                }
                else if (attributeNode.Children[0] is CSharpExpressionIntermediateNode csharpNode)
                {
                    if (csharpNode.Children.Count == 1)
                    {
                        foundToken = csharpNode.Children[0] as IntermediateToken;
                    }
                }
            }
            
            return !string.IsNullOrWhiteSpace(foundToken?.Content) ? foundToken : null;
        }

        private static int LastIndexOf<T>(IList<T> items, Predicate<T> predicate)
        {
            for (var index = items.Count - 1; index >= 0; index--)
            {
                if (predicate(items[index]))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
