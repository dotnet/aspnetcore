// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class InheritsDirectivePass : RazorIRPassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var @class = irDocument.FindPrimaryClass();
            if (@class == null)
            {
                return;
            }

            foreach (var inherits in irDocument.FindDirectiveReferences(InheritsDirective.Directive))
            {
                inherits.Remove();

                var token = ((DirectiveIRNode)inherits.Node).Tokens.FirstOrDefault();
                if (token != null)
                {
                    
                    @class.BaseType = token.Content;
                    break;
                }
            }
        }
    }
}
