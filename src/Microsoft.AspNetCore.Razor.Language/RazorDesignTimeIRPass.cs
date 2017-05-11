// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class RazorDesignTimeIRPass : RazorIRPassBase, IRazorDirectiveClassifierPass
    {
        internal const string DesignTimeVariable = "__o";

        // This needs to run before other directive classifiers.
        public override int Order => -10;

        public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var walker = new DesignTimeHelperWalker();
            walker.VisitDocument(irDocument);
        }

        internal class DesignTimeHelperWalker : RazorIRNodeWalker
        {
            private DesignTimeDirectiveIRNode _designTimeDirectiveIRNode;

            public override void VisitClassDeclaration(ClassDeclarationIRNode node)
            {
                var designTimeHelperDeclaration = new CSharpStatementIRNode();
                RazorIRBuilder.Create(designTimeHelperDeclaration)
                    .Add(new RazorIRToken()
                    {
                        Kind = RazorIRToken.TokenKind.CSharp,
                        Content = $"private static {typeof(object).FullName} {DesignTimeVariable} = null;"
                    });

                node.Children.Insert(0, designTimeHelperDeclaration);

                _designTimeDirectiveIRNode = new DesignTimeDirectiveIRNode();

                VisitDefault(node);

                node.Children.Insert(0, _designTimeDirectiveIRNode);
            }

            public override void VisitDirectiveToken(DirectiveTokenIRNode node)
            {
                _designTimeDirectiveIRNode.Children.Add(node);
            }
        }
    }
}
