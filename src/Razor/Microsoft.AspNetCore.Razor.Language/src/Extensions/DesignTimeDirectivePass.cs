// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal class DesignTimeDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    internal const string DesignTimeVariable = "__o";

    // This needs to run after other directive classifiers. Any DirectiveToken that is not removed
    // by the previous classifiers will have auto-generated design time support.
    public override int Order => DefaultFeatureOrder;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        // Only supports design time. This pass rewrites directives so they will have the right design time
        // behavior and would break things if it ran for runtime.
        if (!documentNode.Options.DesignTime)
        {
            return;
        }

        var walker = new DesignTimeHelperWalker();
        walker.VisitDocument(documentNode);
    }

    internal class DesignTimeHelperWalker : IntermediateNodeWalker
    {
        private DesignTimeDirectiveIntermediateNode _directiveNode;

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            node.Children.Insert(0, new CSharpCodeIntermediateNode()
            {
                Children =
                    {
                        new IntermediateToken()
                        {
                            Kind = TokenKind.CSharp,
                            Content = "#pragma warning disable 0414",
                        }
                    }
            });
            node.Children.Insert(1, new CSharpCodeIntermediateNode()
            {
                Children =
                    {
                        new IntermediateToken()
                        {
                            Kind = TokenKind.CSharp,
                            Content = $"private static {typeof(object).FullName} {DesignTimeVariable} = null;",
                        }
                    }
            });
            node.Children.Insert(2, new CSharpCodeIntermediateNode()
            {
                Children =
                    {
                        new IntermediateToken()
                        {
                            Kind = TokenKind.CSharp,
                            Content = "#pragma warning restore 0414",
                        }
                    }
            });

            _directiveNode = new DesignTimeDirectiveIntermediateNode();

            VisitDefault(node);

            node.Children.Insert(0, _directiveNode);
        }

        public override void VisitDirectiveToken(DirectiveTokenIntermediateNode node)
        {
            _directiveNode.Children.Add(node);
        }
    }
}
