// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class ImplementsDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @class = documentNode.FindPrimaryClass();
            if (@class == null)
            {
                return;
            }

            if (@class.Interfaces == null)
            {
                @class.Interfaces = new List<string>();
            }

            foreach (var implements in documentNode.FindDirectiveReferences(ImplementsDirective.Directive))
            {
                var token = ((DirectiveIntermediateNode)implements.Node).Tokens.FirstOrDefault();
                if (token != null)
                {
                    @class.Interfaces.Add(token.Content);
                }
            }
        }
    }
}
