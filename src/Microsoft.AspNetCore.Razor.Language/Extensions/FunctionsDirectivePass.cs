// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class FunctionsDirectivePass : RazorIRPassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var @class = irDocument.FindPrimaryClass();
            if (@class == null)
            {
                return;
            }

            foreach (var functions in irDocument.FindDirectiveReferences(FunctionsDirective.Directive))
            {
                functions.Remove();

                for (var i = 0; i < functions.Node.Children.Count; i++)
                {
                    @class.Children.Add(functions.Node.Children[i]);
                }
            }
        }
    }
}
