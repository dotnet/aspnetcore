// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentCodeDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @class = documentNode.FindPrimaryClass();
            if (@class == null)
            {
                return;
            }

            foreach (var code in documentNode.FindDirectiveReferences(ComponentCodeDirective.Directive))
            {
                for (var i = 0; i < code.Node.Children.Count; i++)
                {
                    @class.Children.Add(code.Node.Children[i]);
                }
            }
        }
    }
}
