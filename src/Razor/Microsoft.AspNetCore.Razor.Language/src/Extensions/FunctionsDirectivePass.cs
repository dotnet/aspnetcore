// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
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

            var directiveNodes = new List<IntermediateNode>();
            foreach (var functions in documentNode.FindDirectiveReferences(FunctionsDirective.Directive))
            {
                directiveNodes.Add(functions.Node);
            }

            if (FileKinds.IsComponent(codeDocument.GetFileKind()))
            {
                foreach (var code in documentNode.FindDirectiveReferences(ComponentCodeDirective.Directive))
                {
                    directiveNodes.Add(code.Node);
                }
            }

            // Now we have all the directive nodes, we want to add them to the end of the class node in document order.
            var orderedDirectives = directiveNodes.OrderBy(n => n.Source?.AbsoluteIndex);
            foreach (var node in orderedDirectives)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    @class.Children.Add(node.Children[i]);
                }
            }
        }
    }
}
