// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution
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
            private DirectiveTokenHelperIRNode _directiveTokenHelper;

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                var designTimeHelperDeclaration = new CSharpStatementIRNode()
                {
                    Content = $"private static {typeof(object).FullName} {DesignTimeVariable} = null;",
                };

                node.Children.Insert(0, designTimeHelperDeclaration);

                _directiveTokenHelper = new DirectiveTokenHelperIRNode();

                VisitDefault(node);

                node.Children.Insert(0, _directiveTokenHelper);
            }

            public override void VisitDirectiveToken(DirectiveTokenIRNode node)
            {
                _directiveTokenHelper.AddToMethodBody(node);
            }

            private class DirectiveTokenHelperIRNode : RazorIRNode
            {
                private const string DirectiveTokenHelperMethodName = "__RazorDirectiveTokenHelpers__";
                private int _methodBodyIndex = 2;

                public DirectiveTokenHelperIRNode()
                {
                    var disableWarningPragma = new CSharpStatementIRNode()
                    {
                        Content = "#pragma warning disable 219",
                    };
                    Children.Add(disableWarningPragma);

                    var methodStartNode = new CSharpStatementIRNode()
                    {
                        Content = "private void " + DirectiveTokenHelperMethodName + "() {"
                    };
                    Children.Add(methodStartNode);

                    var methodEndNode = new CSharpStatementIRNode()
                    {
                        Content = "}"
                    };
                    Children.Add(methodEndNode);

                    var restoreWarningPragma = new CSharpStatementIRNode()
                    {
                        Content = "#pragma warning restore 219",
                    };
                    Children.Add(restoreWarningPragma);
                }

                public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

                public override RazorIRNode Parent { get; set; }

                public override SourceSpan? Source { get; set; }

                public void AddToMethodBody(RazorIRNode node)
                {
                    Children.Insert(_methodBodyIndex++, node);
                }

                public override void Accept(RazorIRNodeVisitor visitor)
                {
                    visitor.VisitDefault(this);
                }
            }
        }
    }
}
