// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class FunctionsDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @class = documentNode.FindPrimaryClass();
            if (@class == null)
            {
                return;
            }

            foreach (var functions in documentNode.FindDirectiveReferences(FunctionsDirective.Directive))
            {
                for (var i = 0; i < functions.Node.Children.Count; i++)
                {
                    @class.Children.Add(functions.Node.Children[i]);
                }
            }
        }
    }
}
