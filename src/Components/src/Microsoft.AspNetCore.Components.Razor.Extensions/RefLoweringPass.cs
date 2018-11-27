// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class RefLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after component lowering pass
        public override int Order => 50;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            var references = documentNode.FindDescendantReferences<TagHelperPropertyIntermediateNode>();
            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                var node = (TagHelperPropertyIntermediateNode)reference.Node;

                if (node.TagHelper.IsRefTagHelper())
                {
                    reference.Replace(RewriteUsage(@class, reference.Parent, node));
                }
            }
        }

        private IntermediateNode RewriteUsage(ClassDeclarationIntermediateNode classNode, IntermediateNode parent, TagHelperPropertyIntermediateNode node)
        {
            // If we can't get a nonempty attribute name, do nothing because there will
            // already be a diagnostic for empty values
            var identifierToken = DetermineIdentifierToken(node);
            if (identifierToken == null)
            {
                return node;
            }

            // Determine whether this is an element capture or a component capture, and
            // if applicable the type name that will appear in the resulting capture code
            var componentTagHelper = (parent as ComponentExtensionNode)?.Component;
            if (componentTagHelper != null)
            {
                return new RefExtensionNode(identifierToken, componentTagHelper.GetTypeName());
            }
            else
            {
                return new RefExtensionNode(identifierToken);
            }
        }

        private IntermediateToken DetermineIdentifierToken(TagHelperPropertyIntermediateNode attributeNode)
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
    }
}
