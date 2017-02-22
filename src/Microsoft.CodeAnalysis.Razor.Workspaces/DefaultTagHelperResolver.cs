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
        public DefaultTagHelperResolver(bool designTime)
        {
            DesignTime = designTime;
        }

        public bool DesignTime { get; }

        public override TagHelperResolutionResult GetTagHelpers(Compilation compilation, IEnumerable<string> assemblyNameFilters)
        {
            var descriptors = new List<TagHelperDescriptor>();
            var errors = new ErrorSink();

            VisitTagHelpers(compilation, assemblyNameFilters, descriptors, errors);
            VisitViewComponents(compilation, assemblyNameFilters, descriptors, errors);

            var diagnostics = new List<RazorDiagnostic>();
            for (var i = 0; i < errors.Errors.Count; i++)
            {
                var diagnostic = RazorDiagnostic.Create(errors.Errors[i]);
                diagnostics.Add(diagnostic);
            }
            var resolutionResult = new TagHelperResolutionResult(descriptors, diagnostics);

            return resolutionResult;
        }

        private void VisitTagHelpers(Compilation compilation, IEnumerable<string> assemblyNameFilters, List<TagHelperDescriptor> results, ErrorSink errors)
        {
            var types = new List<INamedTypeSymbol>();
            var visitor = TagHelperTypeVisitor.Create(compilation, types);

            VisitCompilation(visitor, compilation);

            var factory = new DefaultTagHelperDescriptorFactory(compilation, DesignTime);

            foreach (var type in types)
            {
                if (assemblyNameFilters.Contains(type.ContainingAssembly.Identity.Name))
                {
                    var descriptors = factory.CreateDescriptors(type, errors);
                    results.AddRange(descriptors);
                }
            }
        }

        private void VisitViewComponents(Compilation compilation, IEnumerable<string> assemblyNameFilters, List<TagHelperDescriptor> results, ErrorSink errors)
        {
            var types = new List<INamedTypeSymbol>();
            var visitor = ViewComponentTypeVisitor.Create(compilation, types);

            VisitCompilation(visitor, compilation);

            var factory = new ViewComponentTagHelperDescriptorFactory(compilation);

            foreach (var type in types)
            {
                try
                {
                    if (assemblyNameFilters.Contains(type.ContainingAssembly.Identity.Name))
                    {
                        var descriptor = factory.CreateDescriptor(type);

                        results.Add(descriptor);
                    }
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
    }
}