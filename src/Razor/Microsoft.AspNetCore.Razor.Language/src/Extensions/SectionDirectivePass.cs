// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class SectionDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        var @class = documentNode.FindPrimaryClass();
        if (@class == null)
        {
            return;
        }

        foreach (var directive in documentNode.FindDirectiveReferences(SectionDirective.Directive))
        {
            var sectionName = ((DirectiveIntermediateNode)directive.Node).Tokens.FirstOrDefault()?.Content;

            var section = new SectionIntermediateNode()
            {
                SectionName = sectionName,
            };

            var i = 0;
            for (; i < directive.Node.Children.Count; i++)
            {
                if (!(directive.Node.Children[i] is DirectiveTokenIntermediateNode))
                {
                    break;
                }
            }

            while (i != directive.Node.Children.Count)
            {
                // Move non-token children over to the section node so we don't have double references to children nodes.
                section.Children.Add(directive.Node.Children[i]);
                directive.Node.Children.RemoveAt(i);
            }

            directive.InsertAfter(section);
        }
    }
}
