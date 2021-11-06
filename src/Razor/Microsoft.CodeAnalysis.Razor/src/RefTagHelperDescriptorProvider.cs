// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.CodeAnalysis.Razor;

internal class RefTagHelperDescriptorProvider : ITagHelperDescriptorProvider
{
    // Run after the component tag helper provider, because later we may want component-type-specific variants of this
    public int Order { get; set; } = 1000;

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

        var elementReference = compilation.GetTypeByMetadataName(ComponentsApi.ElementReference.FullTypeName);
        if (elementReference == null)
        {
            // If we can't find ElementRef, then just bail. We won't be able to compile the
            // generated code anyway.
            return;
        }

        var targetAssembly = context.Items.GetTargetAssembly();
        if (targetAssembly is not null && !SymbolEqualityComparer.Default.Equals(targetAssembly, elementReference.ContainingAssembly))
        {
            return;
        }

        context.Results.Add(CreateRefTagHelper());
    }

    private TagHelperDescriptor CreateRefTagHelper()
    {
        var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Ref.TagHelperKind, "Ref", ComponentsApi.AssemblyName);
        builder.CaseSensitive = true;
        builder.Documentation = ComponentResources.RefTagHelper_Documentation;

        builder.Metadata.Add(ComponentMetadata.SpecialKindKey, ComponentMetadata.Ref.TagHelperKind);
        builder.Metadata.Add(TagHelperMetadata.Common.ClassifyAttributesOnly, bool.TrueString);
        builder.Metadata[TagHelperMetadata.Runtime.Name] = ComponentMetadata.Ref.RuntimeName;

        // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
        // a C# property will crash trying to create the tooltips.
        builder.SetTypeName("Microsoft.AspNetCore.Components.Ref");

        builder.TagMatchingRule(rule =>
        {
            rule.TagName = "*";
            rule.Attribute(attribute =>
            {
                attribute.Name = "@ref";
                attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
            });
        });

        builder.BindAttribute(attribute =>
        {
            attribute.Documentation = ComponentResources.RefTagHelper_Documentation;
            attribute.Name = "@ref";

                // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                // a C# property will crash trying to create the tooltips.
                attribute.SetPropertyName("Ref");
            attribute.TypeName = typeof(object).FullName;
            attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
        });

        return builder.Build();
    }
}
