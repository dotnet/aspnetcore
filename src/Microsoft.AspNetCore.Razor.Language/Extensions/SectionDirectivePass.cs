// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class SectionDirectivePass : RazorIRPassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var @class = irDocument.FindPrimaryClass();
            if (@class == null)
            {
                return;
            }

            foreach (var section in irDocument.FindDirectiveReferences(SectionDirective.Directive))
            {
                var lambdaContent = irDocument.Options.DesignTime ? "__razor_section_writer" : string.Empty;
                var sectionName = ((DirectiveIRNode)section.Node).Tokens.FirstOrDefault()?.Content;

                var builder = RazorIRBuilder.Create(new CSharpCodeIRNode());
                builder.Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
                    Content = $"DefineSection(\"{sectionName}\", async ({lambdaContent}) => {{"
                });
                section.InsertBefore(builder.Build());

                section.InsertBefore(section.Node.Children.Except(((DirectiveIRNode)section.Node).Tokens));

                builder = RazorIRBuilder.Create(new CSharpCodeIRNode());
                builder.Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
                    Content = "});"
                });
                section.InsertAfter(builder.Build());

                section.Remove();
            }
        }
    }
}
