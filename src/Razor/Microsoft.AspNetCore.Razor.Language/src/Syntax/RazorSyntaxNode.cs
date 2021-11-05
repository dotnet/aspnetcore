// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal abstract partial class RazorSyntaxNode : SyntaxNode
{
    public RazorSyntaxNode(GreenNode green, SyntaxNode parent, int position)
        : base(green, parent, position)
    {
    }
}
