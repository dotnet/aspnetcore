// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TagHelperDescriptorCache
    {
        private static readonly MemoryCache<int, TagHelperDescriptor> CachedTagHelperDescriptors = new MemoryCache<int, TagHelperDescriptor>();

        // Disable cache for testing purposes
        internal static bool CacheEnabled { private get; set; } = true;

        public static bool TryGetDescriptor(int hashCode, out TagHelperDescriptor descriptor)
        {
            if (CacheEnabled)
            {
                return CachedTagHelperDescriptors.TryGetValue(hashCode, out descriptor);
            }

            descriptor = default;
            return false;
        }

        public static void Set(int hashCode, TagHelperDescriptor descriptor)
        {
            if (CacheEnabled)
            {
                CachedTagHelperDescriptors.Set(hashCode, descriptor);
            }
        }
    }
}
