// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TagHelperDescriptorBuilderExtensions
    {
        public static void SetTypeName(this TagHelperDescriptorBuilder builder, string typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            builder.Metadata[TagHelperMetadata.Common.TypeName] = typeName;
        }

        public static string GetTypeName(this TagHelperDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Metadata.ContainsKey(TagHelperMetadata.Common.TypeName))
            {
                return builder.Metadata[TagHelperMetadata.Common.TypeName];
            }

            return null;
        }
    }
}
