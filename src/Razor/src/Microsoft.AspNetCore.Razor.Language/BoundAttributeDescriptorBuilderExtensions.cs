// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class BoundAttributeDescriptorBuilderExtensions
    {
        public static void SetPropertyName(this BoundAttributeDescriptorBuilder builder, string propertyName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            builder.Metadata[TagHelperMetadata.Common.PropertyName] = propertyName;
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

        public static void AsDictionary(
            this BoundAttributeDescriptorBuilder builder,
            string attributeNamePrefix,
            string valueTypeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.IsDictionary = true;
            builder.IndexerAttributeNamePrefix = attributeNamePrefix;
            builder.IndexerValueTypeName = valueTypeName;
        }
    }
}
