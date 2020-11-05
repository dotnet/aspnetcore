// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
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
}
