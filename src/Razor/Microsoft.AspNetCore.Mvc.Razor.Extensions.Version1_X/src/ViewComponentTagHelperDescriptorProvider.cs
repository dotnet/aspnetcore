// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public sealed class ViewComponentTagHelperDescriptorProvider : RazorEngineFeatureBase, ITagHelperDescriptorProvider
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

        var vcAttribute = compilation.GetTypeByMetadataName(ViewComponentTypes.ViewComponentAttribute);
        var nonVCAttribute = compilation.GetTypeByMetadataName(ViewComponentTypes.NonViewComponentAttribute);
        if (vcAttribute == null || vcAttribute.TypeKind == TypeKind.Error)
        {
            // Could not find attributes we care about in the compilation. Nothing to do.
            return;
        }

        var types = new List<INamedTypeSymbol>();
        var visitor = new ViewComponentTypeVisitor(vcAttribute, nonVCAttribute, types);

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

        var factory = new ViewComponentTagHelperDescriptorFactory(compilation);
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
