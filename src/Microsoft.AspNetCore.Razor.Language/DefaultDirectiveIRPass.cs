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
            var walker = new DirectiveWalker();
            walker.VisitDocument(irDocument);

            var classNode = walker.ClassNode;
            foreach (var node in walker.FunctionsDirectiveNodes)
            {
                node.Parent.Children.Remove(node);

                foreach (var child in node.Children.Except(node.Tokens))
                {
                    child.Parent = classNode;
                    classNode.Children.Add(child);
                }
            }

            foreach (var node in walker.InheritsDirectiveNodes.Reverse())
            {
                node.Parent.Children.Remove(node);

                var token = node.Tokens.FirstOrDefault();
                if (token != null)
                {
                    classNode.BaseType = token.Content;
                    break;
                }
            }

            foreach (var node in walker.SectionDirectiveNodes)
            {
                var sectionIndex = node.Parent.Children.IndexOf(node);
                node.Parent.Children.Remove(node);

                var defineSectionEndStatement = new CSharpStatementIRNode();
                RazorIRBuilder.Create(defineSectionEndStatement)
                    .Add(new RazorIRToken()
                    {
                        Kind = RazorIRToken.TokenKind.CSharp,
                        Content = "});"
                    });

                node.Parent.Children.Insert(sectionIndex, defineSectionEndStatement);

                foreach (var child in node.Children.Except(node.Tokens).Reverse())
                {
                    node.Parent.Children.Insert(sectionIndex, child);
                }

                var lambdaContent = designTime ? "__razor_section_writer" : string.Empty;
                var sectionName = node.Tokens.FirstOrDefault()?.Content;
                var defineSectionStartStatement = new CSharpStatementIRNode();
                RazorIRBuilder.Create(defineSectionStartStatement)
                    .Add(new RazorIRToken()
                    {
                        Kind = RazorIRToken.TokenKind.CSharp,
                        Content = $"DefineSection(\"{sectionName}\", async ({lambdaContent}) => {{"
                    });

                node.Parent.Children.Insert(sectionIndex, defineSectionStartStatement);
            }
        }

        private class DirectiveWalker : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode ClassNode { get; private set; }

            public IList<DirectiveIRNode> FunctionsDirectiveNodes { get; } = new List<DirectiveIRNode>();

            public IList<DirectiveIRNode> InheritsDirectiveNodes { get; } = new List<DirectiveIRNode>();

            public IList<DirectiveIRNode> SectionDirectiveNodes { get; } = new List<DirectiveIRNode>();

            public override void VisitClassDeclaration(ClassDeclarationIRNode node)
            {
                if (ClassNode == null)
                {
                    ClassNode = node;
                }

                VisitDefault(node);
            }

            public override void VisitDirective(DirectiveIRNode node)
            {
                if (string.Equals(node.Name, CSharpCodeParser.FunctionsDirectiveDescriptor.Directive, StringComparison.Ordinal))
                {
                    FunctionsDirectiveNodes.Add(node);
                }
                else if (string.Equals(node.Name, CSharpCodeParser.InheritsDirectiveDescriptor.Directive, StringComparison.Ordinal))
                {
                    InheritsDirectiveNodes.Add(node);
                }
                else if (string.Equals(node.Name, CSharpCodeParser.SectionDirectiveDescriptor.Directive, StringComparison.Ordinal))
                {
                    SectionDirectiveNodes.Add(node);
                }
            }
        }
    }
}
