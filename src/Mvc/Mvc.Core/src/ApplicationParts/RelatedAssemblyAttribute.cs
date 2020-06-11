// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Specifies a assembly to load as part of MVC's assembly discovery mechanism.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class RelatedAssemblyAttribute : Attribute
    {
        private static readonly Func<string, Assembly> AssemblyLoadFileDelegate = Assembly.LoadFile;

        /// <summary>
        /// Initializes a new instance of <see cref="RelatedAssemblyAttribute"/>.
        /// </summary>
        /// <param name="assemblyFileName">The file name, without extension, of the related assembly.</param>
        public RelatedAssemblyAttribute(string assemblyFileName)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(assemblyFileName));
            }

            AssemblyFileName = assemblyFileName;
        }

        /// <summary>
        /// Gets the assembly file name without extension.
        /// </summary>
        public string AssemblyFileName { get; }

        /// <summary>
        /// Gets <see cref="Assembly"/> instances specified by <see cref="RelatedAssemblyAttribute"/>.
        /// </summary>
        /// <param name="assembly">The assembly containing <see cref="RelatedAssemblyAttribute"/> instances.</param>
        /// <param name="throwOnError">Determines if the method throws if a related assembly could not be located.</param>
        /// <returns>Related <see cref="Assembly"/> instances.</returns>
        public static IReadOnlyList<Assembly> GetRelatedAssemblies(Assembly assembly, bool throwOnError)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            return GetRelatedAssemblies(assembly, throwOnError, File.Exists, AssemblyLoadFileDelegate);
        }

        internal static IReadOnlyList<Assembly> GetRelatedAssemblies(
            Assembly assembly,
            bool throwOnError,
            Func<string, bool> fileExists,
            Func<string, Assembly> loadFile)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            // MVC will specifically look for related parts in the same physical directory as the assembly.
            // No-op if the assembly does not have a location.
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.CodeBase))
            {
                return Array.Empty<Assembly>();
            }

            var attributes = assembly.GetCustomAttributes<RelatedAssemblyAttribute>().ToArray();
            if (attributes.Length == 0)
            {
                return Array.Empty<Assembly>();
            }

            var assemblyName = assembly.GetName().Name;
            var assemblyLocation = GetAssemblyLocation(assembly);
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

            var relatedAssemblies = new List<Assembly>();
            for (var i = 0; i < attributes.Length; i++)
            {
                var attribute = attributes[i];
                if (string.Equals(assemblyName, attribute.AssemblyFileName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        Resources.FormatRelatedAssemblyAttribute_AssemblyCannotReferenceSelf(nameof(RelatedAssemblyAttribute), assemblyName));
                }

                var relatedAssemblyLocation = Path.Combine(assemblyDirectory, attribute.AssemblyFileName + ".dll");
                if (!fileExists(relatedAssemblyLocation))
                {
                    if (throwOnError)
                    {
                        throw new FileNotFoundException(
                            Resources.FormatRelatedAssemblyAttribute_CouldNotBeFound(attribute.AssemblyFileName, assemblyName, assemblyDirectory),
                            relatedAssemblyLocation);
                    }
                    else
                    {
                        continue;
                    }
                }

                var relatedAssembly = loadFile(relatedAssemblyLocation);
                relatedAssemblies.Add(relatedAssembly);
            }

            return relatedAssemblies;
        }

        internal static string GetAssemblyLocation(Assembly assembly)
        {
            if (Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out var result) && 
                result.IsFile && string.IsNullOrWhiteSpace(result.Fragment))
            {
                return result.LocalPath;
            }

            return assembly.Location;
        }
    }
}
