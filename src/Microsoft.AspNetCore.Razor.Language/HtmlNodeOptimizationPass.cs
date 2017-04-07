// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class HtmlNodeOptimizationPass : IRazorSyntaxTreePass
    {
        public RazorEngine Engine { get; set; }

        public int Order => 100;

        public RazorSyntaxTree Execute(RazorCodeDocument codeDocument, RazorSyntaxTree syntaxTree)
        {
            var conditionalAttributeCollapser = new ConditionalAttributeCollapser();
            var rewritten = conditionalAttributeCollapser.Rewrite(syntaxTree.Root);

            var whitespaceRewriter = new WhiteSpaceRewriter();
            rewritten = whitespaceRewriter.Rewrite(rewritten);

            var rewrittenSyntaxTree = RazorSyntaxTree.Create(rewritten, syntaxTree.Source, syntaxTree.Diagnostics, syntaxTree.Options);
            return rewrittenSyntaxTree;
        }
    }
}
