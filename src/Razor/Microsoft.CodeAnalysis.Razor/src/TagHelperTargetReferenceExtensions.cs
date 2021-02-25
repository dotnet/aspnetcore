// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class TagHelperTargetReferenceExtensions
    {
        private static readonly object TargetAssemblyKey = new object();

        public static MetadataReference? GetTargetMetadataReference(this ItemCollection items)
        {
            if (items.Count == 0 || items[TargetAssemblyKey] is not MetadataReference reference)
            {
                return null;
            }

            return reference;
        }

        public static void SetTargetMetadataReference(this ItemCollection items, MetadataReference reference)
        {
            items[TargetAssemblyKey] = reference;
        }
    }
}
