// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Razor.Precompilation;

namespace Microsoft.AspNet.Mvc.Razor.Internal
{
    public static class RazorFileInfoCollections
    {
        private static readonly Type RazorFileInfoCollectionType = typeof(RazorFileInfoCollection);

        public static IEnumerable<RazorFileInfoCollection> GetFileInfoCollections(IEnumerable<Assembly> assemblies)
        {
            return assemblies
                .SelectMany(assembly => assembly.ExportedTypes)
                .Where(IsValidRazorFileInfoCollection)
                .Select(Activator.CreateInstance)
                .Cast<RazorFileInfoCollection>();
        }


        public static bool IsValidRazorFileInfoCollection(Type type)
        {
            return 
                RazorFileInfoCollectionType.IsAssignableFrom(type) &&
                !type.GetTypeInfo().IsAbstract &&
                !type.GetTypeInfo().ContainsGenericParameters;
        }
    }
}
