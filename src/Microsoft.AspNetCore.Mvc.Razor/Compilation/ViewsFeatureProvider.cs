// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// An <see cref="IApplicationFeatureProvider{TFeature}"/> for <see cref="ViewsFeature"/>.
    /// </summary>
    public class ViewsFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        public static readonly string PrecompiledViewsAssemblySuffix = ".PrecompiledViews";

        /// <summary>
        /// Gets the namespace for the <see cref="ViewInfoContainer"/> type in the view assembly.
        /// </summary>
        public static readonly string ViewInfoContainerNamespace = "AspNetCore";

        /// <summary>
        /// Gets the type name for the view collection type in the view assembly.
        /// </summary>
        public static readonly string ViewInfoContainerTypeName = "__PrecompiledViewCollection";

        private static readonly string FullyQualifiedManifestTypeName = ViewInfoContainerNamespace + "." + ViewInfoContainerTypeName;

        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
        {
            foreach (var assemblyPart in parts.OfType<AssemblyPart>())
            {
                var viewContainer = GetManifest(assemblyPart);
                if (viewContainer == null)
                {
                    continue;
                }

                foreach (var item in viewContainer.ViewInfos)
                {
                    feature.Views[item.Path] = item.Type;
                }
            }
        }

        /// <summary>
        /// Gets the type of <see cref="ViewInfoContainer"/> for the specified <paramref name="assemblyPart"/>.
        /// </summary>
        /// <param name="assemblyPart">The <see cref="AssemblyPart"/>.</param>
        /// <returns>The <see cref="ViewInfoContainer"/> <see cref="Type"/>.</returns>
        protected virtual ViewInfoContainer GetManifest(AssemblyPart assemblyPart)
        {
            var type = CompiledViewManfiest.GetManifestType(assemblyPart, FullyQualifiedManifestTypeName);
            if (type != null)
            {
                return (ViewInfoContainer)Activator.CreateInstance(type);
            }

            return null;
        }
    }
}
