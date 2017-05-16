// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TagHelperDescriptorExtensions
    {
        public static string GetTypeName(this TagHelperDescriptor descriptor)
        {
            descriptor.Metadata.TryGetValue(TagHelperDescriptorBuilder.TypeNameKey, out var typeName);

            return typeName;
        }

        public static bool IsDefaultKind(this TagHelperDescriptor descriptor)
            => string.Equals(descriptor.Kind, TagHelperDescriptorBuilder.DescriptorKind, StringComparison.Ordinal);
    }
}