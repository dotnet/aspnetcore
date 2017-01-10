// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        public override async Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync(Project project, CancellationToken cancellationToken = default(CancellationToken))
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

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
            var factory = new DefaultTagHelperDescriptorFactory(compilation);

            foreach (var type in types)
            {
                results.AddRange(factory.CreateDescriptors(type.ContainingAssembly.Identity.GetDisplayName(), type, errors));
            }

            return results;
        }

        // Visits top-level types and finds interface implementations.
        private class Visitor : SymbolVisitor
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
                if (symbol.AllInterfaces.Contains(_interface))
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
        }
    }
}
