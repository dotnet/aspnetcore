// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    public sealed class DefaultTagHelperDescriptorProvider : RazorEngineFeatureBase, ITagHelperDescriptorProvider
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

            var iTagHelper = compilation.GetTypeByMetadataName(TagHelperTypes.ITagHelper);
            if (iTagHelper == null || iTagHelper.TypeKind == TypeKind.Error)
            {
                // Could not find attributes we care about in the compilation. Nothing to do.
                return;
            }

            var types = new List<INamedTypeSymbol>();
            var visitor = new TagHelperTypeVisitor(iTagHelper, types);

            if ((context.DiscoveryMode & TagHelperDiscoveryMode.CurrentAssembly) == TagHelperDiscoveryMode.CurrentAssembly)
            {
                visitor.Visit(compilation.Assembly.GlobalNamespace);
            }

            if ((context.DiscoveryMode & TagHelperDiscoveryMode.References) == TagHelperDiscoveryMode.References)
            {
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
            }

            var factory = new DefaultTagHelperDescriptorFactory(compilation, context.IncludeDocumentation, context.ExcludeHidden);
            for (var i = 0; i < types.Count; i++)
            {
                var descriptor = factory.CreateDescriptor(types[i]);

                if (descriptor != null)
                {
                    context.Results.Add(descriptor);
                }
            }
        }

        private bool IsTagHelperAssembly(IAssemblySymbol assembly)
        {
            return assembly.Name != null && !assembly.Name.StartsWith("System.", StringComparison.Ordinal);
        }
    }
}
