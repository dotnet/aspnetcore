// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Razor;

// Rewrites type names to use the 'global::' prefix for identifiers.
//
// This is useful when we're generating code in a different namespace than
// what the user code lives in. When we synthesize a namespace it's easy to have
// clashes.
internal class GlobalQualifiedTypeNameRewriter : TypeNameRewriter
{
    // List of names to ignore.
    //
    // NOTE: this is the list of type parameters defined on the component.
    private readonly HashSet<string> _ignore;

    public GlobalQualifiedTypeNameRewriter(ICollection<string> ignore)
    {
        _ignore = new HashSet<string>(ignore);
    }

    public override string Rewrite(string typeName)
    {
        var parsed = SyntaxFactory.ParseTypeName(typeName);
        var rewritten = (TypeSyntax)new Visitor(_ignore).Visit(parsed);
        return rewritten.ToFullString();
    }

    private class Visitor : CSharpSyntaxRewriter
    {
        private readonly HashSet<string> _ignore;

        public Visitor(HashSet<string> ignore)
        {
            _ignore = ignore;
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

            return SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(CSharp.SyntaxKind.GlobalKeyword)), node);
        }
    }
}
