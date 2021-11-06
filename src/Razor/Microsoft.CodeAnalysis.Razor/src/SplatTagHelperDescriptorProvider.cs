// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.CodeAnalysis.Razor;

internal class SplatTagHelperDescriptorProvider : ITagHelperDescriptorProvider
{
    // Order doesn't matter
    public int Order { get; set; }

    public RazorEngine Engine { get; set; }

    public void Execute(TagHelperDescriptorProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var compilation = context.GetCompilation();
        if (compilation == null)
        {
            return;
        }

        var renderTreeBuilder = compilation.GetTypeByMetadataName(ComponentsApi.RenderTreeBuilder.FullTypeName);
        if (renderTreeBuilder == null)
        {
            // If we can't find RenderTreeBuilder, then just bail. We won't be able to compile the
            // generated code anyway.
            return;
        }

        var targetAssembly = context.Items.GetTargetAssembly();
        if (targetAssembly is not null && !SymbolEqualityComparer.Default.Equals(targetAssembly, renderTreeBuilder.ContainingAssembly))
        {
            return;
        }

        context.Results.Add(CreateSplatTagHelper());
    }

    private TagHelperDescriptor CreateSplatTagHelper()
    {
        var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Splat.TagHelperKind, "Attributes", ComponentsApi.AssemblyName);
        builder.CaseSensitive = true;
        builder.Documentation = ComponentResources.SplatTagHelper_Documentation;

        builder.Metadata.Add(ComponentMetadata.SpecialKindKey, ComponentMetadata.Splat.TagHelperKind);
        builder.Metadata.Add(TagHelperMetadata.Common.ClassifyAttributesOnly, bool.TrueString);
        builder.Metadata[TagHelperMetadata.Runtime.Name] = ComponentMetadata.Splat.RuntimeName;

        // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
        // a C# property will crash trying to create the tooltips.
        builder.SetTypeName("Microsoft.AspNetCore.Components.Attributes");

        builder.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.Attribute(attribute =>
            {
                attribute.Name = "@attributes";
                attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
            });
        });

        builder.BindAttribute(attribute =>
        {
            attribute.Documentation = ComponentResources.SplatTagHelper_Documentation;
            attribute.Name = "@attributes";

                // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                // a C# property will crash trying to create the tooltips.
                attribute.SetPropertyName("Attributes");
            attribute.TypeName = typeof(object).FullName;
            attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
        });

        return builder.Build();
    }
}
