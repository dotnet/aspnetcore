// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal class ViewCssScopePass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Runs after taghelpers are bound
        public override int Order => 110;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var cssScope = codeDocument.GetCssScope();
            if (string.IsNullOrEmpty(cssScope))
            {
                return;
            }

            if (!string.Equals(documentNode.DocumentKind, "mvc.1.0.view", StringComparison.Ordinal) &&
                !string.Equals(documentNode.DocumentKind, "mvc.1.0.razor-page", StringComparison.Ordinal))
            {
                return;
            }

            var scopeWithSeparator = " " + cssScope;
            var nodes = documentNode.FindDescendantNodes<HtmlContentIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                ProcessElement(nodes[i], scopeWithSeparator);
            }
        }

        private void ProcessElement(HtmlContentIntermediateNode node, string cssScope)
        {
            // Add a minimized attribute whose name is simply the CSS scope
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                if (child is IntermediateToken token && token.IsHtml)
                {
                    var content = token.Content;
                    if (content.StartsWith("<", StringComparison.Ordinal) && !content.StartsWith("</", StringComparison.Ordinal))
                    {
                        node.Children.Insert(i + 1, new IntermediateToken()
                        {
                            Content = cssScope,
                            Kind = TokenKind.Html,
                            Source = null
                        });
                        i++;
                    }
                }
            }
        }
    }
}
