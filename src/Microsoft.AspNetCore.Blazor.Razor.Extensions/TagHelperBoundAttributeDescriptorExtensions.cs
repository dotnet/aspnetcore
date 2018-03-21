// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal static class TagHelperBoundAttributeDescriptorExtensions
    {
        public static bool IsDelegateProperty(this BoundAttributeDescriptor attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var key = ComponentTagHelperDescriptorProvider.DelegateSignatureMetadata;
            return 
                attribute.Metadata.TryGetValue(key, out var value) &&
                string.Equals(value, bool.TrueString);
        }
    }
}
