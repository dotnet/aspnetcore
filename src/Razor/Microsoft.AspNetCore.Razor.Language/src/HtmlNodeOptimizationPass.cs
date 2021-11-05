// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language;

internal class HtmlNodeOptimizationPass : RazorEngineFeatureBase, IRazorSyntaxTreePass
{
    public int Order => 100;

    public RazorSyntaxTree Execute(RazorCodeDocument codeDocument, RazorSyntaxTree syntaxTree)
    {
        if (codeDocument == null)
        {
            throw new ArgumentNullException(nameof(codeDocument));
        }

        if (syntaxTree == null)
        {
            throw new ArgumentNullException(nameof(syntaxTree));
        }

        var whitespaceRewriter = new WhitespaceRewriter();
        var rewritten = whitespaceRewriter.Visit(syntaxTree.Root);

        var rewrittenSyntaxTree = RazorSyntaxTree.Create(rewritten, syntaxTree.Source, syntaxTree.Diagnostics, syntaxTree.Options);
        return rewrittenSyntaxTree;
    }
}
