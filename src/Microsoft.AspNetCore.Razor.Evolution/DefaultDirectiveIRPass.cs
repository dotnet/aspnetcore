// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultDirectiveIRPass : RazorIRPassBase
    {
        public override int Order => RazorIRPass.DefaultDirectiveClassifierOrder;

        public override DocumentIRNode ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDocumentDependency(syntaxTree);

            var parserOptions = syntaxTree.Options;

            var designTime = parserOptions.DesignTimeMode;
            var walker = new DirectiveWalker(designTime);
            walker.VisitDocument(irDocument);

            return irDocument;
        }

        private class DirectiveWalker : RazorIRNodeWalker
        {
            private ClassDeclarationIRNode _classNode;
            private readonly bool _designTime;

            public DirectiveWalker(bool designTime)
            {
                _designTime = designTime;
            }

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                if (_classNode == null)
                {
                    _classNode = node;
                }

                VisitDefault(node);
            }

            public override void VisitDirective(DirectiveIRNode node)
            {
                if (string.Equals(node.Name, CSharpCodeParser.FunctionsDirectiveDescriptor.Name, StringComparison.Ordinal))
                {
                    node.Parent.Children.Remove(node);

                    foreach (var child in node.Children.Except(node.Tokens))
                    {
                        child.Parent = _classNode;
                        _classNode.Children.Add(child);
                    }
                }
                else if (string.Equals(node.Name, CSharpCodeParser.InheritsDirectiveDescriptor.Name, StringComparison.Ordinal))
                {
                    node.Parent.Children.Remove(node);

                    var token = node.Tokens.FirstOrDefault();

                    if (token != null)
                    {
                        _classNode.BaseType = token.Content;
                    }
                }
                else if (string.Equals(node.Name, CSharpCodeParser.SectionDirectiveDescriptor.Name, StringComparison.Ordinal))
                {
                    var sectionIndex = node.Parent.Children.IndexOf(node);
                    node.Parent.Children.Remove(node);

                    var defineSectionEndStatement = new CSharpStatementIRNode()
                    {
                        Content = "});",
                    };
                    node.Parent.Children.Insert(sectionIndex, defineSectionEndStatement);

                    foreach (var child in node.Children.Except(node.Tokens).Reverse())
                    {
                        node.Parent.Children.Insert(sectionIndex, child);
                    }

                    var lambdaContent = _designTime ? "__razor_section_writer" : string.Empty;
                    var sectionName = node.Tokens.FirstOrDefault()?.Content;
                    var defineSectionStartStatement = new CSharpStatementIRNode()
                    {
                        Content = /* ORIGINAL: DefineSectionMethodName */ $"DefineSection(\"{sectionName}\", async ({lambdaContent}) => {{",
                    };

                    node.Parent.Children.Insert(sectionIndex, defineSectionStartStatement);
                }
            }
        }
    }
}
