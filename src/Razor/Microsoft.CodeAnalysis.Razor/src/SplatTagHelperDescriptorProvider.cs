// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.CodeAnalysis.Razor
{
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

            if ((context.DiscoveryMode & TagHelperDiscoveryMode.References) != TagHelperDiscoveryMode.References)
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
}
