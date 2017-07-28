// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    public static class TestTagHelperDescriptorBuilderExtensions
    {
        public static TagHelperDescriptorBuilder TypeName(this TagHelperDescriptorBuilder builder, string typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.SetTypeName(typeName);

            return builder;
        }

        public static TagHelperDescriptorBuilder DisplayName(this TagHelperDescriptorBuilder builder, string displayName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.DisplayName = displayName;

            return builder;
        }

        public static TagHelperDescriptorBuilder AllowChildTag(this TagHelperDescriptorBuilder builder, string allowedChild)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AllowChildTag(childTagBuilder => childTagBuilder.Name = allowedChild);

            return builder;
        }

        public static TagHelperDescriptorBuilder TagOutputHint(this TagHelperDescriptorBuilder builder, string hint)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.TagOutputHint = hint;

            return builder;
        }

        public static TagHelperDescriptorBuilder Documentation(this TagHelperDescriptorBuilder builder, string documentation)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Documentation = documentation;

            return builder;
        }

        public static TagHelperDescriptorBuilder AddMetadata(this TagHelperDescriptorBuilder builder, string key, string value)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Metadata[key] = value;

            return builder;
        }

        public static TagHelperDescriptorBuilder AddDiagnostic(this TagHelperDescriptorBuilder builder, RazorDiagnostic diagnostic)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Diagnostics.Add(diagnostic);

            return builder;
        }

        public static TagHelperDescriptorBuilder BoundAttributeDescriptor(
            this TagHelperDescriptorBuilder builder,
            Action<BoundAttributeDescriptorBuilder> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.BindAttribute(configure);

            return builder;
        }

        public static TagHelperDescriptorBuilder TagMatchingRuleDescriptor(
            this TagHelperDescriptorBuilder builder,
            Action<TagMatchingRuleDescriptorBuilder> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.TagMatchingRule(configure);

            return builder;
        }
    }
}
