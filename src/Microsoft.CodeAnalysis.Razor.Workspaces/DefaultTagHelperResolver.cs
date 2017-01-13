// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        public DefaultTagHelperResolver(bool designTime)
        {
            DesignTime = designTime;
        }

        public bool DesignTime { get; }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpers(Compilation compilation)
        {
            var results = new List<TagHelperDescriptor>();

            // If ITagHelper isn't defined, then we couldn't possibly find anything.
            var @interface = compilation.GetTypeByMetadataName(TagHelperTypes.ITagHelper);
            if (@interface == null)
            {
                return results;
            }

            var types = new List<INamedTypeSymbol>();
            var visitor = new Visitor(@interface, types);

            visitor.Visit(compilation.Assembly.GlobalNamespace);

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    visitor.Visit(assembly.GlobalNamespace);
                }
            }

            var errors = new ErrorSink();
            var factory = new DefaultTagHelperDescriptorFactory(compilation, DesignTime);

            foreach (var type in types)
            {
                results.AddRange(factory.CreateDescriptors(type.ContainingAssembly.Identity.GetDisplayName(), type, errors));
            }

            return results;
        }

        // Visits top-level types and finds interface implementations.
        internal class Visitor : SymbolVisitor
        {
            private INamedTypeSymbol _interface;
            private List<INamedTypeSymbol> _results;

            public Visitor(INamedTypeSymbol @interface, List<INamedTypeSymbol> results)
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
                return symbol.DeclaredAccessibility == Accessibility.Public &&
                    !symbol.IsAbstract &&
                    !symbol.IsGenericType &&
                    symbol.AllInterfaces.Contains(_interface);
            }
        }
    }
}