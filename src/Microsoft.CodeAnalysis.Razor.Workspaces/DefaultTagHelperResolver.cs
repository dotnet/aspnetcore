// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        private static readonly Version SupportedVCTHMvcVersion = new Version(1, 1);
        private readonly string ViewComponentAssembly;

        public DefaultTagHelperResolver(bool designTime) : this(designTime, ViewComponentTypes.Assembly)
        {
        }

        // Internal for testing
        internal DefaultTagHelperResolver(bool designTime, string viewComponentAssembly)
        {
            DesignTime = designTime;
            ViewComponentAssembly = viewComponentAssembly;
        }

        public bool DesignTime { get; }

        public override IReadOnlyList<TagHelperDescriptor> GetTagHelpers(Compilation compilation)
        {
            var results = new List<TagHelperDescriptor>();
            var errors = new ErrorSink();

            VisitTagHelpers(compilation, results, errors);
            VisitViewComponents(compilation, results, errors);

            return results;
        }

        private void VisitTagHelpers(Compilation compilation, List<TagHelperDescriptor> results, ErrorSink errors)
        {
            var @interface = compilation.GetTypeByMetadataName(TagHelperTypes.ITagHelper);
            if (@interface == null)
            {
                // If ITagHelper isn't defined, then we couldn't possibly find anything.
                return;
            }

            var types = new List<INamedTypeSymbol>();
            var visitor = new TagHelperVisitor(@interface, types);

            VisitCompilation(visitor, compilation);

            var factory = new DefaultTagHelperDescriptorFactory(compilation, DesignTime);

            foreach (var type in types)
            {
                var descriptors = factory.CreateDescriptors(type, errors);
                results.AddRange(descriptors);
            }
        }

        private void VisitViewComponents(Compilation compilation, List<TagHelperDescriptor> results, ErrorSink errors)
        {
            var mvcViewFeaturesAssembly = compilation.References
                .Select(reference => compilation.GetAssemblyOrModuleSymbol(reference))
                .OfType<IAssemblySymbol>()
                .FirstOrDefault(assembly => string.Equals(assembly.Identity.Name, ViewComponentAssembly, StringComparison.Ordinal));

            if (mvcViewFeaturesAssembly == null || mvcViewFeaturesAssembly.Identity.Version < SupportedVCTHMvcVersion)
            {
                return;
            }

            var viewComponentAttributeSymbol = compilation.GetTypeByMetadataName(ViewComponentTypes.ViewComponentAttribute);
            var nonViewComponentAttributeSymbol = compilation.GetTypeByMetadataName(ViewComponentTypes.NonViewComponentAttribute);
            var types = new List<INamedTypeSymbol>();
            var visitor = new ViewComponentVisitor(viewComponentAttributeSymbol, viewComponentAttributeSymbol, types);

            VisitCompilation(visitor, compilation);

            var factory = new ViewComponentTagHelperDescriptorFactory(compilation);

            foreach (var type in types)
            {
                try
                {
                    var descriptor = factory.CreateDescriptor(type);

                    results.Add(descriptor);
                }
                catch (Exception ex)
                {
                    errors.OnError(SourceLocation.Zero, ex.Message, length: 0);
                }
            }
        }

        private static void VisitCompilation(SymbolVisitor visitor, Compilation compilation)
        {
            visitor.Visit(compilation.Assembly.GlobalNamespace);

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    visitor.Visit(assembly.GlobalNamespace);
                }
            }
        }

        // Visits top-level types and finds interface implementations.
        internal class TagHelperVisitor : SymbolVisitor
        {
            private INamedTypeSymbol _interface;
            private List<INamedTypeSymbol> _results;

            public TagHelperVisitor(INamedTypeSymbol @interface, List<INamedTypeSymbol> results)
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

        internal class ViewComponentVisitor : SymbolVisitor
        {
            private INamedTypeSymbol _viewComponentAttribute;
            private INamedTypeSymbol _nonViewComponentAttribute;
            private List<INamedTypeSymbol> _results;

            public ViewComponentVisitor(
                INamedTypeSymbol viewComponentAttribute,
                INamedTypeSymbol nonViewComponentAttribute,
                List<INamedTypeSymbol> results)
            {
                _viewComponentAttribute = viewComponentAttribute;
                _nonViewComponentAttribute = nonViewComponentAttribute;
                _results = results;
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (IsViewComponent(symbol))
                {
                    _results.Add(symbol);
                }

                if (symbol.DeclaredAccessibility != Accessibility.Public)
                {
                    return;
                }

                foreach (var member in symbol.GetTypeMembers())
                {
                    Visit(member);
                }
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var member in symbol.GetMembers())
                {
                    Visit(member);
                }
            }

            internal bool IsViewComponent(INamedTypeSymbol symbol)
            {
                if (symbol.DeclaredAccessibility != Accessibility.Public ||
                    symbol.IsAbstract ||
                    symbol.IsGenericType ||
                    AttributeIsDefined(symbol, _nonViewComponentAttribute))
                {
                    return false;
                }

                return symbol.Name.EndsWith(ViewComponentTypes.ViewComponentSuffix) ||
                    AttributeIsDefined(symbol, _viewComponentAttribute);
            }

            private static bool AttributeIsDefined(INamedTypeSymbol type, INamedTypeSymbol queryAttribute)
            {
                if (type == null)
                {
                    return false;
                }

                var attribute = type.GetAttributes().Where(a => a.AttributeClass == queryAttribute).FirstOrDefault();

                if (attribute != null)
                {
                    return true;
                }

                return AttributeIsDefined(type.BaseType, queryAttribute);
            }
        }
    }
}