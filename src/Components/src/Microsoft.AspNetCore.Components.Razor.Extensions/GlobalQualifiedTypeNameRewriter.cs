// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Razor
{
    // Rewrites type names to use the 'global::' prefix for identifiers.
    //
    // This is useful when we're generating code in a different namespace than
    // what the user code lives in. When we synthesize a namespace it's easy to have
    // clashes.
    internal class GlobalQualifiedTypeNameRewriter : CSharpSyntaxRewriter
    {
        // List of names to ignore.
        //
        // NOTE: this is the list of type parameters defined on the component.
        private readonly HashSet<string> _ignore;

        public GlobalQualifiedTypeNameRewriter(IEnumerable<string> ignore)
        {
            _ignore = new HashSet<string>(ignore);
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            return base.Visit(node);
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (node.Parent is QualifiedNameSyntax)
            {
                return base.VisitQualifiedName(node);
            }

            // Need to rewrite postorder so we can rewrite the names of generic type arguments.
            node = (QualifiedNameSyntax)base.VisitQualifiedName(node);

            // Rewriting these is complicated, best to just tostring and parse again.
            return SyntaxFactory.ParseTypeName("global::" + node.ToString());
        }
        
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (_ignore.Contains(node.ToString()))
            {
                return node;
            }

            if (node.Parent != null)
            {
                return node;
            }

            return SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), node);
        }
    }
}
