// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// An <see cref="IApplicationFeatureProvider{TFeature}"/> for <see cref="ViewsFeature"/>.
    /// </summary>
    public class ViewsFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        /// <summary>
        /// Gets the suffix for the view assembly.
        /// </summary>
        public static readonly string PrecompiledViewsAssemblySuffix = ".PrecompiledViews";

        /// <summary>
        /// Gets the namespace for the <see cref="ViewInfoContainer"/> type in the view assembly.
        /// </summary>
        public static readonly string ViewInfoContainerNamespace = "AspNetCore";

        /// <summary>
        /// Gets the type name for the view collection type in the view assembly.
        /// </summary>
        public static readonly string ViewInfoContainerTypeName = "__PrecompiledViewCollection";

        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
        {
            foreach (var assemblyPart in parts.OfType<AssemblyPart>())
            {
                var viewInfoContainerTypeName = GetViewInfoContainerType(assemblyPart);
                if (viewInfoContainerTypeName == null)
                {
                    continue;
                }

                var viewContainer = (ViewInfoContainer)Activator.CreateInstance(viewInfoContainerTypeName);

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
        protected virtual Type GetViewInfoContainerType(AssemblyPart assemblyPart)
        {
#if NETSTANDARD1_6
            if (!assemblyPart.Assembly.IsDynamic && !string.IsNullOrEmpty(assemblyPart.Assembly.Location))
            {
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
                        System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(precompiledAssemblyFilePath);
                    }
                    catch (FileLoadException)
                    {
                        // Don't throw if assembly cannot be loaded. This can happen if the file is not a managed assembly.
                    }
                }
            }
#endif

            var precompiledAssemblyName = new AssemblyName(assemblyPart.Assembly.FullName);
            precompiledAssemblyName.Name = precompiledAssemblyName.Name + PrecompiledViewsAssemblySuffix;

            var typeName = $"{ViewInfoContainerNamespace}.{ViewInfoContainerTypeName},{precompiledAssemblyName}";
            return Type.GetType(typeName);
        }
    }
}
