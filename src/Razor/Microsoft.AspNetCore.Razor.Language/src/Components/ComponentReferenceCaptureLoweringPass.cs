// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentReferenceCaptureLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Run after component lowering pass
    public override int Order => 50;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var @namespace = documentNode.FindPrimaryNamespace();
        var @class = documentNode.FindPrimaryClass();
        if (@namespace == null || @class == null)
        {
            // Nothing to do, bail. We can't function without the standard structure.
            return;
        }

        var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();
        for (var i = 0; i < references.Count; i++)
        {
            var reference = references[i];
            var node = (TagHelperDirectiveAttributeIntermediateNode)reference.Node;

            if (node.TagHelper.IsRefTagHelper())
            {
                reference.Replace(RewriteUsage(@class, reference.Parent, node));
            }
        }
    }

    private IntermediateNode RewriteUsage(ClassDeclarationIntermediateNode classNode, IntermediateNode parent, TagHelperDirectiveAttributeIntermediateNode node)
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
        var componentTagHelper = (parent as ComponentIntermediateNode)?.Component;
        if (componentTagHelper != null)
        {
            return new ReferenceCaptureIntermediateNode(identifierToken, componentTagHelper.GetTypeName());
        }
        else
        {
            return new ReferenceCaptureIntermediateNode(identifierToken);
        }
    }

    private IntermediateToken DetermineIdentifierToken(TagHelperDirectiveAttributeIntermediateNode attributeNode)
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
