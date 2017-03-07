// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Razor
{
    public static class TagHelpers
    {
        public static IReadOnlyList<TagHelperDescriptor> GetTagHelpers(Compilation compilation)
        {
            var results = new List<TagHelperDescriptor>();
            var errors = new ErrorSink();

            VisitTagHelpers(compilation, results, errors);
            VisitViewComponents(compilation, results, errors);

            return results;
        }

        private static void VisitTagHelpers(Compilation compilation, List<TagHelperDescriptor> results, ErrorSink errors)
        {
            var types = new List<INamedTypeSymbol>();
            var visitor = TagHelperTypeVisitor.Create(compilation, types);

            VisitCompilation(visitor, compilation);

            var factory = new DefaultTagHelperDescriptorFactory(compilation, designTime: false);

            foreach (var type in types)
            {
                var descriptor = factory.CreateDescriptor(type);
                if (descriptor != null)
                {
                    results.Add(descriptor);
                }
            }
        }

        private static void VisitViewComponents(Compilation compilation, List<TagHelperDescriptor> results, ErrorSink errors)
        {
            var types = new List<INamedTypeSymbol>();
            var visitor = ViewComponentTypeVisitor.Create(compilation, types);

            VisitCompilation(visitor, compilation);

            var factory = new ViewComponentTagHelperDescriptorFactory(compilation);

            foreach (var type in types)
            {
                try
                {
                    var descriptor = factory.CreateDescriptor(type);

                    if (descriptor != null)
                    {
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
