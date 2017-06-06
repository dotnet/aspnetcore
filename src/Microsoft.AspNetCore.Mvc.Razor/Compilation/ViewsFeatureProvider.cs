// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.Primitives;

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
                var viewAttributes = GetViewAttributes(assemblyPart);
                foreach (var attribute in viewAttributes)
                {
                    var relativePath = ViewPath.NormalizePath(attribute.Path);
                    var viewDescriptor = new CompiledViewDescriptor
                    {
                        ExpirationTokens = Array.Empty<IChangeToken>(),
                        RelativePath = relativePath,
                        ViewAttribute = attribute,
                        IsPrecompiled = true,
                    };

                    feature.ViewDescriptors.Add(viewDescriptor);
                }
            }
        }

        /// <summary>
        /// Gets the sequence of <see cref="RazorViewAttribute"/> instances associated with the specified <paramref name="assemblyPart"/>.
        /// </summary>
        /// <param name="assemblyPart">The <see cref="AssemblyPart"/>.</param>
        /// <returns>The sequence of <see cref="RazorViewAttribute"/> instances.</returns>
        protected virtual IEnumerable<RazorViewAttribute> GetViewAttributes(AssemblyPart assemblyPart)
        {
            if (assemblyPart == null)
            {
                throw new ArgumentNullException(nameof(assemblyPart));
            }

            var featureAssembly = CompiledViewManfiest.GetFeatureAssembly(assemblyPart);
            if (featureAssembly != null)
            {
                return featureAssembly.GetCustomAttributes<RazorViewAttribute>();
            }

            return Enumerable.Empty<RazorViewAttribute>();
        }
    }
}
