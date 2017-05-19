// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class BoundAttributeDescriptorExtensions
    {
        public static string GetPropertyName(this BoundAttributeDescriptor descriptor)
        {
            descriptor.Metadata.TryGetValue(BoundAttributeDescriptorBuilder.PropertyNameKey, out var propertyName);

            return propertyName;
        }

        public static bool IsDefaultKind(this BoundAttributeDescriptor descriptor)
            => string.Equals(descriptor.Kind, BoundAttributeDescriptorBuilder.DescriptorKind, StringComparison.Ordinal);
    }
}