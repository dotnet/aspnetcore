// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentCssScopePass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after component lowering pass
        public override int Order => 50;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (!IsComponentDocument(documentNode))
            {
                return;
            }

            var cssScope = codeDocument.GetCssScope();
            if (string.IsNullOrEmpty(cssScope))
            {
                return;
            }

            var nodes = documentNode.FindDescendantNodes<MarkupElementIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                ProcessElement(nodes[i], cssScope);
            }
        }

        private void ProcessElement(MarkupElementIntermediateNode node, string cssScope)
        {
            // Add a minimized attribute whose name is simply the CSS scope
            node.Children.Add(new HtmlAttributeIntermediateNode
            {
                AttributeName = cssScope,
                Prefix = cssScope,
                Suffix = string.Empty,
            });
        }
    }
}
