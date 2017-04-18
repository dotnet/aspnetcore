// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTagHelperResolver : TagHelperResolver
    {
        public DefaultTagHelperResolver(bool designTime)
        {
            DesignTime = designTime;
        }

        public bool DesignTime { get; }

        public override TagHelperResolutionResult GetTagHelpers(Compilation compilation, IEnumerable<string> assemblyNameFilters)
        {
            var descriptors = new List<TagHelperDescriptor>();

            VisitTagHelpers(compilation, assemblyNameFilters, descriptors);
            VisitViewComponents(compilation, assemblyNameFilters, descriptors);

            var diagnostics = new List<RazorDiagnostic>();
            var resolutionResult = new TagHelperResolutionResult(descriptors, diagnostics);

            return resolutionResult;
        }

        private void VisitTagHelpers(Compilation compilation, IEnumerable<string> assemblyNameFilters, List<TagHelperDescriptor> results)
        {
            var types = new List<INamedTypeSymbol>();
            var visitor = TagHelperTypeVisitor.Create(compilation, types);

            VisitCompilation(visitor, compilation);

            var factory = new DefaultTagHelperDescriptorFactory(compilation, DesignTime);

            foreach (var type in types)
            {
                if (assemblyNameFilters == null || assemblyNameFilters.Contains(type.ContainingAssembly.Identity.Name))
                {
                    var descriptor = factory.CreateDescriptor(type);

                    if (descriptor != null)
                    {
                        results.Add(descriptor);
                    }
                }
            }
        }

        private void VisitViewComponents(Compilation compilation, IEnumerable<string> assemblyNameFilters, List<TagHelperDescriptor> results)
        {
            var types = new List<INamedTypeSymbol>();
            var visitor = ViewComponentTypeVisitor.Create(compilation, types);

            VisitCompilation(visitor, compilation);

            var factory = new ViewComponentTagHelperDescriptorFactory(compilation);
            foreach (var type in types)
            {
                if (assemblyNameFilters == null || assemblyNameFilters.Contains(type.ContainingAssembly.Identity.Name))
                {
                    var descriptor = factory.CreateDescriptor(type);

                    results.Add(descriptor);
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
    }
}