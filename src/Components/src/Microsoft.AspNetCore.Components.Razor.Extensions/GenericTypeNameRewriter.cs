// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class GenericTypeNameRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, Binding> _bindings;

        public GenericTypeNameRewriter(Dictionary<string, Binding> bindings)
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
                    var replacement = binding?.Content == null ? typeof(object).FullName : binding.Content;
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

        public class Binding
        {
            public BoundAttributeDescriptor Attribute { get; set; }

            public string Content { get; set; }

            public ComponentTypeArgumentExtensionNode Node { get; set; }
        }
    }
}
