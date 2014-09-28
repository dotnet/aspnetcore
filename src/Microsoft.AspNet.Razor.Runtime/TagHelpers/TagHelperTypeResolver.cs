// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class that locates valid <see cref="ITagHelper"/>s within an assembly.
    /// </summary>
    public class TagHelperTypeResolver
    {
        private static readonly TypeInfo ITagHelperTypeInfo = typeof(ITagHelper).GetTypeInfo();

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperTypeResolver"/> class.
        /// </summary>
        public TagHelperTypeResolver()
        {
        }

        /// <summary>
        /// Loads an <see cref="Assembly"/> using the given <paramref name="name"/> and resolves
        /// all valid <see cref="ITagHelper"/> <see cref="Type"/>s.
        /// </summary>
        /// <param name="name">The name of an <see cref="Assembly"/> to search.</param>
        /// <returns>An <see cref="IEnumerable{Type}"/> of valid <see cref="ITagHelper"/> <see cref="Type"/>s.</returns>        
        public IEnumerable<Type> Resolve(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    Resources.TagHelperTypeResolver_TagHelperAssemblyNameCannotBeEmptyOrNull,
                    nameof(name));
            }

            var assemblyName = new AssemblyName(name);
            var libraryTypes = GetLibraryDefinedTypes(assemblyName);
            var validTagHelpers = libraryTypes.Where(IsTagHelper);

            // Convert from TypeInfo[] to Type[]
            return validTagHelpers.Select(type => type.AsType());
        }

        // Internal for testing, don't want to be loading assemblies during a test.
        internal virtual IEnumerable<TypeInfo> GetLibraryDefinedTypes(AssemblyName assemblyName)
        {
            try
            {
                var assembly = Assembly.Load(assemblyName);

                return assembly.DefinedTypes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    Resources.FormatTagHelperTypeResolver_CannotResolveTagHelperAssembly(assemblyName.Name,
                                                                                         ex.Message));
            }
        }

        private static bool IsTagHelper(TypeInfo typeInfo)
        {
            return typeInfo.IsPublic &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.IsGenericType &&
                   !typeInfo.IsNested &&
                   ITagHelperTypeInfo.IsAssignableFrom(typeInfo);
        }
    }
}