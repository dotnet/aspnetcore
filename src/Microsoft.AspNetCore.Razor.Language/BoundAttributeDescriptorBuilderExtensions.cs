// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class BoundAttributeDescriptorBuilderExtensions
    {
        public static BoundAttributeDescriptorBuilder SetPropertyName(this BoundAttributeDescriptorBuilder builder, string propertyName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            builder.AddMetadata(TagHelperMetadata.Common.PropertyName, propertyName);

            return builder;
        }

        public static string GetPropertyName(this BoundAttributeDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Metadata.ContainsKey(TagHelperMetadata.Common.PropertyName))
            {
                return builder.Metadata[TagHelperMetadata.Common.PropertyName];
            }

            return null;
        }
    }
}
