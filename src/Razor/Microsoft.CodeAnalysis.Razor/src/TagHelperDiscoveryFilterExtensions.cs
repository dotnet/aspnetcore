// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class TagHelperDiscoveryFilterExtensions
    {
        private static readonly object TagHelperDiscoveryModeKey = new object();

        public static TagHelperDiscoveryFilter GetTagHelperDiscoveryFilter(this ItemCollection items)
        {
            if (items.Count == 0 || items[TagHelperDiscoveryModeKey] is not TagHelperDiscoveryFilter filter)
            {
                return TagHelperDiscoveryFilter.Default;
            }

            return filter;
        }

        public static void SetTagHelperDiscoveryFilter(this ItemCollection items, TagHelperDiscoveryFilter filter)
        {
            items[TagHelperDiscoveryModeKey] = filter;
        }
    }
}
