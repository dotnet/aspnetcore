// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using System;

namespace Microsoft.AspNetCore.Components.Razor
{
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

            context.Results.Add(CreateRefTagHelper());
        }

        private TagHelperDescriptor CreateRefTagHelper()
        {
            var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.Ref.TagHelperKind, "Ref", ComponentsApi.AssemblyName);
            builder.Documentation = Resources.RefTagHelper_Documentation;

            builder.Metadata.Add(BlazorMetadata.SpecialKindKey, BlazorMetadata.Ref.TagHelperKind);
            builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.Ref.RuntimeName;

            // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
            // a C# property will crash trying to create the tooltips.
            builder.SetTypeName("Microsoft.AspNetCore.Components.Ref");

            builder.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.Attribute(attribute =>
                {
                    attribute.Name = "ref";
                });
            });

            builder.BindAttribute(attribute =>
            {
                attribute.Documentation = Resources.RefTagHelper_Documentation;
                attribute.Name = "ref";

                // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                // a C# property will crash trying to create the tooltips.
                attribute.SetPropertyName("Ref");
                attribute.TypeName = typeof(object).FullName;
            });

            return builder.Build();
        }
    }
}
