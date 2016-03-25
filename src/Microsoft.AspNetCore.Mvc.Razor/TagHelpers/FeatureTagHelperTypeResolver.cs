// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    /// <summary>
    /// Resolves tag helper types from the <see cref="ApplicationPartManager.ApplicationParts"/>
    /// of the application.
    /// </summary>
    public class FeatureTagHelperTypeResolver : TagHelperTypeResolver
    {
        private readonly TagHelperFeature _feature;

        /// <summary>
        /// Initializes a new <see cref="FeatureTagHelperTypeResolver"/> instance.
        /// </summary>
        /// <param name="manager">The <see cref="ApplicationPartManager"/> of the application.</param>
        public FeatureTagHelperTypeResolver(ApplicationPartManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            _feature = new TagHelperFeature();
            manager.PopulateFeature(_feature);
        }

        /// <inheritdoc />
        protected override IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            var results = new List<TypeInfo>();
            for (var i = 0; i < _feature.TagHelpers.Count; i++)
            {
                var tagHelperAssemblyName = _feature.TagHelpers[i].Assembly.GetName();

                if (AssemblyNameComparer.OrdinalIgnoreCase.Equals(tagHelperAssemblyName, assemblyName))
                {
                    results.Add(_feature.TagHelpers[i]);
                }
            }

            return results;
        }

        /// <inheritdoc />
        protected sealed override bool IsTagHelper(TypeInfo typeInfo)
        {
            // Return true always as we have already decided what types are tag helpers when GetExportedTypes
            // gets called.
            return true;
        }

        private class AssemblyNameComparer : IEqualityComparer<AssemblyName>
        {
            public static readonly IEqualityComparer<AssemblyName> OrdinalIgnoreCase = new AssemblyNameComparer();

            private AssemblyNameComparer()
            {
            }

            public bool Equals(AssemblyName x, AssemblyName y)
            {
                // Ignore case because that's what Assembly.Load does.
                return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.CultureName ?? string.Empty, y.CultureName ?? string.Empty, StringComparison.Ordinal);
            }

            public int GetHashCode(AssemblyName obj)
            {
                var hashCode = 0;
                if (obj.Name != null)
                {
                    hashCode ^= obj.Name.GetHashCode();
                }

                hashCode ^= (obj.CultureName ?? string.Empty).GetHashCode();
                return hashCode;
            }
        }
    }
}
