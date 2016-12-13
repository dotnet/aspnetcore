// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultDirectiveIRPass : IRazorIRPass
    {
        public RazorEngine Engine { get; set; }

        public int Order => 150;

        public DocumentIRNode Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var walker = new DirectiveWalker();
            walker.VisitDefault(irDocument);

            return irDocument;
        }

        private class DirectiveWalker : RazorIRNodeWalker
        {
            private ClassDeclarationIRNode _classNode;

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
                    foreach (var child in node.Children.Except(node.Tokens))
                    {
                        child.Parent = _classNode;
                        _classNode.Children.Add(child);
                    }
                }
                else if (string.Equals(node.Name, CSharpCodeParser.InheritsDirectiveDescriptor.Name, StringComparison.Ordinal))
                {
                    var token = node.Tokens.FirstOrDefault();

                    if (token != null)
                    {
                        _classNode.BaseType = token.Content;
                    }
                }
            }
        }
    }
}
