// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal static class TagHelperTargetReferenceExtensions
    {
        private static readonly object TargetAssemblyKey = new object();

        public static IAssemblySymbol? GetTargetAssembly(this ItemCollection items)
        {
            if (items.Count == 0 || items[TargetAssemblyKey] is not IAssemblySymbol assembly)
            {
                return null;
            }

            return assembly;
        }

        public static void SetTargetAssembly(this ItemCollection items, IAssemblySymbol assembly)
        {
            items[TargetAssemblyKey] = assembly;
        }
    }
}
