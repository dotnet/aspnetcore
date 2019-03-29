// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentMarkupEncodingPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
    {
        // Runs after ComponentMarkupBlockPass
        public override int Order => 10010;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (!IsComponentDocument(documentNode))
            {
                return;
            }

            if (documentNode.Options.DesignTime)
            {
                // Nothing to do during design time.
                return;
            }

            var rewriter = new Rewriter();
            rewriter.Visit(documentNode);
        }

        private class Rewriter : IntermediateNodeWalker
        {
            // Markup content in components are rendered in one of the following two ways,
            // AddContent - we encode it when used with prerendering and inserted into the DOM in a safe way (low perf impact)
            // AddMarkupContent - renders the content directly as markup (high perf impact)
            // Because of this, we want to use AddContent as much as possible.
            //
            // We want to use AddMarkupContent to avoid aggresive encoding during prerendering.
            // Specifically, when one of the following characters are in the content,
            // 1. New lines (\r, \n), tabs(\t) - so they get rendered as actual new lines, tabs instead of &#xA;
            // 2. Ampersands (&) - so that HTML entities are rendered correctly without getting encoded
            // 3. Any character outside the ASCII range

            private static readonly char[] EncodedCharacters = new[] { '\r', '\n', '\t', '&' };

            public override void VisitHtml(HtmlContentIntermediateNode node)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    if (!(child is IntermediateToken token) || !token.IsHtml)
                    {
                        // We only care about Html tokens.
                        continue;
                    }

                    for (var j = 0; j < token.Content.Length; j++)
                    {
                        var ch = token.Content[j];
                        // ASCII range is 0 - 127
                        if (ch > 127 || EncodedCharacters.Contains(ch))
                        {
                            node.SetEncoded();
                            return;
                        }
                    }
                }
            }
        }
    }
}
