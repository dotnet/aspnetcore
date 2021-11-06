// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class InheritsDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        var @class = documentNode.FindPrimaryClass();
        if (@class == null)
        {
            return;
        }

        foreach (var inherits in documentNode.FindDirectiveReferences(InheritsDirective.Directive))
        {
            var token = ((DirectiveIntermediateNode)inherits.Node).Tokens.FirstOrDefault();
            if (token != null)
            {
                @class.BaseType = token.Content;
                break;
            }
        }
    }
}
