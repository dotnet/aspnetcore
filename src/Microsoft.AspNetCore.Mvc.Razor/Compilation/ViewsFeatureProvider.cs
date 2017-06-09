// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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

            var featureAssembly = GetFeatureAssembly(assemblyPart);
            if (featureAssembly != null)
            {
                return featureAssembly.GetCustomAttributes<RazorViewAttribute>();
            }

            return Enumerable.Empty<RazorViewAttribute>();
        }

        private static Assembly GetFeatureAssembly(AssemblyPart assemblyPart)
        {
            if (assemblyPart.Assembly.IsDynamic || string.IsNullOrEmpty(assemblyPart.Assembly.Location))
            {
                return null;
            }

            var precompiledAssemblyFileName = assemblyPart.Assembly.GetName().Name
                + PrecompiledViewsAssemblySuffix
                + ".dll";
            var precompiledAssemblyFilePath = Path.Combine(
                Path.GetDirectoryName(assemblyPart.Assembly.Location),
                precompiledAssemblyFileName);

            if (File.Exists(precompiledAssemblyFilePath))
            {
                try
                {
                    return Assembly.LoadFile(precompiledAssemblyFilePath);
                }
                catch (FileLoadException)
                {
                    // Don't throw if assembly cannot be loaded. This can happen if the file is not a managed assembly.
                }
            }

            return null;
        }
    }
}
