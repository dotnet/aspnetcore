// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal static class TagHelperDescriptorMatchingConventions
    {
        public static bool CanMatchName(this BoundAttributeDescriptor descriptor, string name)
        {
            return IsFullNameMatch(descriptor, name) || IsIndexerNameMatch(descriptor, name);
        }

        public static bool IsFullNameMatch(this BoundAttributeDescriptor descriptor, string name)
        {
            return string.Equals(descriptor.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsIndexerNameMatch(this BoundAttributeDescriptor descriptor, string name)
        {
            return descriptor.IndexerNamePrefix != null &&
                !IsFullNameMatch(descriptor, name) &&
                name.StartsWith(descriptor.IndexerNamePrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMatch(this RequiredAttributeDescriptor descriptor, string attributeName, string attributeValue)
        {
            var nameMatches = false;
            if (descriptor.NameComparison == RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
            {
                nameMatches = string.Equals(descriptor.Name, attributeName, StringComparison.OrdinalIgnoreCase);
            }
            else if (descriptor.NameComparison == RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)
            {
                // attributeName cannot equal the Name if comparing as a PrefixMatch.
                nameMatches = attributeName.Length != descriptor.Name.Length &&
                    attributeName.StartsWith(descriptor.Name, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Debug.Assert(false, "Unknown name comparison.");
            }

            if (!nameMatches)
            {
                return false;
            }

            switch (descriptor.ValueComparison)
            {
                case RequiredAttributeDescriptor.ValueComparisonMode.None:
                    return true;
                case RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch: // Value starts with
                    return attributeValue.StartsWith(descriptor.Value, StringComparison.Ordinal);
                case RequiredAttributeDescriptor.ValueComparisonMode.SuffixMatch: // Value ends with
                    return attributeValue.EndsWith(descriptor.Value, StringComparison.Ordinal);
                case RequiredAttributeDescriptor.ValueComparisonMode.FullMatch: // Value equals
                    return string.Equals(attributeValue, descriptor.Value, StringComparison.Ordinal);
                default:
                    Debug.Assert(false, "Unknown value comparison.");
                    return false;
            }
        }
    }
}
