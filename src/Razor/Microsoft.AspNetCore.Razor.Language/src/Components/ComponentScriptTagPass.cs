// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentScriptTagPass : ComponentIntermediateNodePassBase, IRazorDocumentClassifierPass
{
    // Run as soon as possible after the Component rewrite pass
    public override int Order => 5;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var visitor = new Visitor();
        visitor.Visit(documentNode);
    }

    private class Visitor : IntermediateNodeWalker
    {
        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            // Disallow <script> in components as per #552
            if (string.Equals(node.TagName, "script", StringComparison.OrdinalIgnoreCase))
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    // We allow you to suppress this error like:
                    // <script suppress-error="BL9992" />
                    var attribute = node.Children[i] as HtmlAttributeIntermediateNode;
                    if (attribute != null &&
                        attribute.AttributeName == "suppress-error" &&
                        attribute.Children.Count == 1 &&
                        attribute.Children[0] is HtmlAttributeValueIntermediateNode value &&
                        value.Children.Count == 1 &&
                        value.Children[0] is IntermediateToken token &&
                        token.IsHtml &&
                        string.Equals(token.Content, "BL9992", StringComparison.Ordinal))
                    {
                        node.Children.RemoveAt(i);
                        return;
                    }
                }

                var diagnostic = ComponentDiagnosticFactory.Create_DisallowedScriptTag(node.Source);
                node.Diagnostics.Add(diagnostic);
            }

            base.VisitDefault(node);
        }
    }
}
