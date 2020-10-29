// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class KeyTagHelperDescriptorProvider : ITagHelperDescriptorProvider
    {
        // Run after the component tag helper provider
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

            if ((context.DiscoveryMode & TagHelperDiscoveryMode.References) != TagHelperDiscoveryMode.References)
            {
                return;
            }

            var renderTreeBuilderType = compilation.GetTypeByMetadataName(ComponentsApi.RenderTreeBuilder.FullTypeName);
            if (renderTreeBuilderType == null)
            {
                // If we can't find RenderTreeBuilder, then just bail. We won't be able to compile the
                // generated code anyway.
                return;
            }

            context.Results.Add(CreateKeyTagHelper());
        }

        private TagHelperDescriptor CreateKeyTagHelper()
        {
            var builder = TagHelperDescriptorBuilder.Create(ComponentMetadata.Key.TagHelperKind, "Key", ComponentsApi.AssemblyName);
            builder.CaseSensitive = true;
            builder.Documentation = ComponentResources.KeyTagHelper_Documentation;

            builder.Metadata.Add(ComponentMetadata.SpecialKindKey, ComponentMetadata.Key.TagHelperKind);
            builder.Metadata.Add(TagHelperMetadata.Common.ClassifyAttributesOnly, bool.TrueString);
            builder.Metadata[TagHelperMetadata.Runtime.Name] = ComponentMetadata.Key.RuntimeName;

            // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
            // a C# property will crash trying to create the tooltips.
            builder.SetTypeName("Microsoft.AspNetCore.Components.Key");

            builder.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.Attribute(attribute =>
                {
                    attribute.Name = "@key";
                    attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
                });
            });

            builder.BindAttribute(attribute =>
            {
                attribute.Documentation = ComponentResources.KeyTagHelper_Documentation;
                attribute.Name = "@key";

                // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                // a C# property will crash trying to create the tooltips.
                attribute.SetPropertyName("Key");
                attribute.TypeName = typeof(object).FullName;
                attribute.Metadata[ComponentMetadata.Common.DirectiveAttribute] = bool.TrueString;
            });

            return builder.Build();
        }
    }
}
