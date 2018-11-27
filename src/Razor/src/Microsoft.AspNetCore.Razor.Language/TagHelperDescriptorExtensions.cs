// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TagHelperDescriptorExtensions
    {
        public static string GetTypeName(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            tagHelper.Metadata.TryGetValue(TagHelperMetadata.Common.TypeName, out var typeName);
            return typeName;
        }

        public static bool IsDefaultKind(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return string.Equals(tagHelper.Kind, TagHelperConventions.DefaultKind, StringComparison.Ordinal);
        }

        public static bool KindUsesDefaultTagHelperRuntime(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            tagHelper.Metadata.TryGetValue(TagHelperMetadata.Runtime.Name, out var value);
            return string.Equals(TagHelperConventions.DefaultKind, value, StringComparison.Ordinal);
        }
    }
}