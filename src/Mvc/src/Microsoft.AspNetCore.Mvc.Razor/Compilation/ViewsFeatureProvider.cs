// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// An <see cref="IApplicationFeatureProvider{TFeature}"/> for <see cref="ViewsFeature"/>.
    /// </summary>
    [Obsolete("This type is obsolete and will be removed in a future version. See " + nameof(IRazorCompiledItemProvider) + " for alternatives.")]
    public class ViewsFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        public static readonly string PrecompiledViewsAssemblySuffix = ".PrecompiledViews";

        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
        {
            foreach (var assemblyPart in parts.OfType<AssemblyPart>())
            {
                var viewAttributes = GetViewAttributes(assemblyPart)
                    .Select(attribute => (Attribute: attribute, RelativePath: ViewPath.NormalizePath(attribute.Path)));

                var duplicates = viewAttributes.GroupBy(a => a.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(g => g.Count() > 1);

                if (duplicates != null)
                {
                    // Ensure parts do not specify views with differing cases. This is not supported
                    // at runtime and we should flag at as such for precompiled views.
                    var viewsDifferingInCase = string.Join(Environment.NewLine, duplicates.Select(d => d.RelativePath));

                    var message = string.Join(
                        Environment.NewLine,
                        Resources.RazorViewCompiler_ViewPathsDifferOnlyInCase,
                        viewsDifferingInCase);
                    throw new InvalidOperationException(message);
                }

                foreach (var (attribute, relativePath) in viewAttributes)
                {
                    var viewDescriptor = new CompiledViewDescriptor
                    {
                        ExpirationTokens = Array.Empty<IChangeToken>(),
                        RelativePath = relativePath,
                        ViewAttribute = attribute,
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
            if (assemblyPart.Assembly.IsDynamic || string.IsNullOrEmpty((string)assemblyPart.Assembly.Location))
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