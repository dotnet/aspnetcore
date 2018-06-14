// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor
{
    // Visits top-level types and finds interface implementations.
    internal class TagHelperTypeVisitor : SymbolVisitor
    {
        private readonly INamedTypeSymbol _interface;
        private readonly List<INamedTypeSymbol> _results;

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
            return
                symbol.TypeKind != TypeKind.Error &&
                symbol.DeclaredAccessibility == Accessibility.Public &&
                !symbol.IsAbstract &&
                !symbol.IsGenericType &&
                symbol.AllInterfaces.Contains(_interface);
        }
    }
}
