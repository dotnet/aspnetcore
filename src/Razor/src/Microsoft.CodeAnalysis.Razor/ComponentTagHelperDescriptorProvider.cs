// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    public class ComponentTagHelperDescriptorProvider : RazorEngineFeatureBase, ITagHelperDescriptorProvider
    {
        public int Order { get; set; }

        public void Execute(TagHelperDescriptorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var compilation = context.GetCompilation();
            if (compilation == null)
            {
                // No compilation, nothing to do.
                return;
            }

            var componentSymbol = compilation.GetTypeByMetadataName(TagHelperTypes.IComponent);
            if (componentSymbol == null || componentSymbol.TypeKind == TypeKind.Error)
            {
                // Could not find attributes we care about in the compilation. Nothing to do.
                return;
            }

            var types = new List<INamedTypeSymbol>();
            var visitor = new ComponentTypeVisitor(componentSymbol, types);

            // We always visit the global namespace.
            visitor.Visit(compilation.Assembly.GlobalNamespace);

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    if (IsTagHelperAssembly(assembly))
                    {
                        visitor.Visit(assembly.GlobalNamespace);
                    }
                }
            }

            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var descriptor = CreateDescriptor(type);

                if (descriptor != null)
                {
                    context.Results.Add(descriptor);
                }
            }
        }

        private static TagHelperDescriptor CreateDescriptor(INamedTypeSymbol type)
        {
            var typeName = type.ToDisplayString(DefaultTagHelperDescriptorFactory.FullNameTypeDisplayFormat);
            var assemblyName = type.ContainingAssembly.Identity.Name;

            var descriptorBuilder = TagHelperDescriptorBuilder.Create(TagHelperConventions.ComponentKind, typeName, assemblyName);
            descriptorBuilder.SetTypeName(typeName);
            descriptorBuilder.Metadata[TagHelperMetadata.Runtime.Name] = TagHelperConventions.ComponentKind;

            // Components have very simple matching rules. The type name (short) matches the tag name.
            descriptorBuilder.TagMatchingRule(r => r.TagName = type.Name);

            var descriptor = descriptorBuilder.Build();
            return descriptor;
        }

        private bool IsTagHelperAssembly(IAssemblySymbol assembly)
        {
            return assembly.Name != null && !assembly.Name.StartsWith("System.", StringComparison.Ordinal);
        }

        // Visits top-level types and finds interface implementations.
        internal class ComponentTypeVisitor : SymbolVisitor
        {
            private readonly INamedTypeSymbol _componentSymbol;
            private readonly List<INamedTypeSymbol> _results;

            public ComponentTypeVisitor(INamedTypeSymbol componentSymbol, List<INamedTypeSymbol> results)
            {
                _componentSymbol = componentSymbol;
                _results = results;
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (IsComponent(symbol))
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

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                // This as a simple yet high-value optimization that excludes the vast majority of
                // assemblies that (by definition) can't contain a component.
                if (symbol.Name != null && !symbol.Name.StartsWith("System.", StringComparison.Ordinal))
                {
                    Visit(symbol.GlobalNamespace);
                }
            }

            internal bool IsComponent(INamedTypeSymbol symbol)
            {
                return
                    symbol.DeclaredAccessibility == Accessibility.Public &&
                    !symbol.IsAbstract &&
                    symbol.AllInterfaces.Contains(_componentSymbol);
            }
        }
    }
}
