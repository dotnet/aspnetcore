// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor
{
    // Visits top-level types and finds interface implementations.
    internal class TagHelperTypeVisitor : SymbolVisitor
    {
        private INamedTypeSymbol _interface;
        private List<INamedTypeSymbol> _results;

        public static TagHelperTypeVisitor Create(Compilation compilation, List<INamedTypeSymbol> results)
        {
            var @interface = compilation.GetTypeByMetadataName(TagHelperTypes.ITagHelper);
            return new TagHelperTypeVisitor(@interface, results);
        }

        public TagHelperTypeVisitor(INamedTypeSymbol @interface, List<INamedTypeSymbol> results)
        {
            _interface = @interface;
            _results = results;
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (IsTagHelper(symbol))
            {
                _results.Add(symbol);
            }
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                Visit(member);
            }
        }

        internal bool IsTagHelper(INamedTypeSymbol symbol)
        {
            if (_interface == null)
            {
                return false;
            }

            return
                symbol.TypeKind != TypeKind.Error &&
                symbol.DeclaredAccessibility == Accessibility.Public &&
                !symbol.IsAbstract &&
                !symbol.IsGenericType &&
                symbol.AllInterfaces.Contains(_interface);
        }
    }
}
