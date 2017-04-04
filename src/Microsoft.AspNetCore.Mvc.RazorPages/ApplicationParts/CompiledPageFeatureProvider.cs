// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// An <see cref="IApplicationFeatureProvider{TFeature}"/> for <see cref="ViewsFeature"/>.
    /// </summary>
    public class CompiledPageFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        /// <summary>
        /// Gets the namespace for the <see cref="ViewInfoContainer"/> type in the view assembly.
        /// </summary>
        public static readonly string CompiledPageManifestNamespace = "AspNetCore";

        /// <summary>
        /// Gets the type name for the view collection type in the view assembly.
        /// </summary>
        public static readonly string CompiledPageManifestTypeName = "__CompiledRazorPagesManifest";

        private static readonly string FullyQualifiedManifestTypeName =
            CompiledPageManifestNamespace + "." + CompiledPageManifestTypeName;

        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
        {
            foreach (var item in GetCompiledPageInfo(parts))
            {
                feature.Views.Add(item.Path, item.CompiledType);
            }
        }

        /// <summary>
        /// Gets the sequence of <see cref="CompiledPageInfo"/> from <paramref name="parts"/>.
        /// </summary>
        /// <param name="parts">The <see cref="ApplicationPart"/>s</param>
        /// <returns>The sequence of <see cref="CompiledPageInfo"/>.</returns>
        public static IEnumerable<CompiledPageInfo> GetCompiledPageInfo(IEnumerable<ApplicationPart> parts)
        {
            return parts.OfType<AssemblyPart>()
                .Select(part => CompiledViewManfiest.GetManifestType(part, FullyQualifiedManifestTypeName))
                .Where(type => type != null)
                .Select(type => (CompiledPageManifest)Activator.CreateInstance(type))
                .SelectMany(manifest => manifest.CompiledPages);
        }
    }
}
