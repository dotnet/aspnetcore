// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Razor;

internal class GenericTypeNameRewriter : TypeNameRewriter
{
    private readonly Dictionary<string, string> _bindings;

    public GenericTypeNameRewriter(Dictionary<string, string> bindings)
    {
        _bindings = bindings;
    }

    public override string Rewrite(string typeName)
    {
        var parsed = SyntaxFactory.ParseTypeName(typeName);
        var rewritten = (TypeSyntax)new Visitor(_bindings).Visit(parsed);
        return rewritten.ToFullString();
    }

    private class Visitor : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, string> _bindings;

        public Visitor(Dictionary<string, string> bindings)
        {
            _bindings = bindings;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            // We can handle a single IdentifierNameSyntax at the top level (like 'TItem)
            // OR a GenericNameSyntax recursively (like `List<T>`)
            if (node is IdentifierNameSyntax identifier && !(identifier.Parent is QualifiedNameSyntax))
            {
                if (_bindings.TryGetValue(identifier.Identifier.Text, out var binding))
                {
                    // If we don't have a valid replacement, use object. This will make the code at least reasonable
                    // compared to leaving the type parameter in place.
                    //
                    // We add our own diagnostics for missing/invalid type parameters anyway.
                    var replacement = binding == null ? typeof(object).FullName : binding;
                    return identifier.Update(SyntaxFactory.Identifier(replacement));
                }
            }

            return base.Visit(node);
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            var args = node.TypeArgumentList.Arguments;
            for (var i = 0; i < args.Count; i++)
            {
                var typeArgument = args[i];
                args = args.Replace(typeArgument, (TypeSyntax)Visit(typeArgument));
            }

            return node.WithTypeArgumentList(node.TypeArgumentList.WithArguments(args));
        }
    }
}
