// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultDirectiveIRPass : RazorIRPassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var parserOptions = irDocument.Options;

            var designTime = parserOptions.DesignTime;

            var classNode = irDocument.FindPrimaryClass();
            if (classNode == null)
            {
                return;
            }

            foreach (var functions in irDocument.FindDirectiveReferences(CSharpCodeParser.FunctionsDirectiveDescriptor))
            {
                functions.Remove();

                for (var i =0; i < functions.Node.Children.Count; i++)
                {
                    classNode.Children.Add(functions.Node.Children[i]);
                }
            }

            foreach (var inherits in irDocument.FindDirectiveReferences(CSharpCodeParser.InheritsDirectiveDescriptor).Reverse())
            {
                inherits.Remove();

                var token = ((DirectiveIRNode)inherits.Node).Tokens.FirstOrDefault();
                if (token != null)
                {
                    classNode.BaseType = token.Content;
                    break;
                }
            }

            foreach (var section in irDocument.FindDirectiveReferences(CSharpCodeParser.SectionDirectiveDescriptor))
            {
                var lambdaContent = designTime ? "__razor_section_writer" : string.Empty;
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
