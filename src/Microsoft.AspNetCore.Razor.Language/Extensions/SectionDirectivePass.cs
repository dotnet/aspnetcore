// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class SectionDirectivePass : RazorIRPassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var @class = irDocument.FindPrimaryClass();
            if (@class == null)
            {
                return;
            }

            foreach (var directive in irDocument.FindDirectiveReferences(SectionDirective.Directive))
            {
                var sectionName = ((DirectiveIRNode)directive.Node).Tokens.FirstOrDefault()?.Content;

                var section = new SectionIRNode()
                {
                    Name = sectionName,
                };

                var i = 0;
                for (; i < directive.Node.Children.Count; i++)
                {
                    if (!(directive.Node.Children[i] is DirectiveTokenIRNode))
                    {
                        break;
                    }
                }

                for (; i < directive.Node.Children.Count; i++)
                {
                    section.Children.Add(directive.Node.Children[i]);
                }

                directive.Replace(section);
            }
        }
    }
}
