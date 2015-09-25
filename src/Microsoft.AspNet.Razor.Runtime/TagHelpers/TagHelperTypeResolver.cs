// Copyright (c) .NET Foundation. All rights reserved.
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
        private static readonly ITypeInfo ITagHelperTypeInfo = new RuntimeTypeInfo(typeof(ITagHelper).GetTypeInfo());

        /// <summary>
        /// Locates valid <see cref="ITagHelper"/> types from the <see cref="Assembly"/> named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of an <see cref="Assembly"/> to search.</param>
        /// <param name="documentLocation">The <see cref="SourceLocation"/> of the associated
        /// <see cref="Parser.SyntaxTree.SyntaxTreeNode"/> responsible for the current <see cref="Resolve"/> call.
        /// </param>
        /// <param name="errorSink">The <see cref="ErrorSink"/> used to record errors found when resolving
        /// <see cref="ITagHelper"/> types.</param>
        /// <returns>An <see cref="IEnumerable{ITypeInfo}"/> of valid <see cref="ITagHelper"/> types.</returns>
        public IEnumerable<ITypeInfo> Resolve(
            string name,
            SourceLocation documentLocation,
            ErrorSink errorSink)
        {
            if (errorSink == null)
            {
                throw new ArgumentNullException(nameof(errorSink));
            }

            if (string.IsNullOrEmpty(name))
            {
                var errorLength = name == null ? 1 : Math.Max(name.Length, 1);
                errorSink.OnError(
                    documentLocation,
                    Resources.TagHelperTypeResolver_TagHelperAssemblyNameCannotBeEmptyOrNull,
                    errorLength);

                return Enumerable.Empty<ITypeInfo>();
            }

            var assemblyName = new AssemblyName(name);

            IEnumerable<ITypeInfo> libraryTypes;
            try
            {
                libraryTypes = GetTopLevelExportedTypes(assemblyName);
            }
            catch (Exception ex)
            {
                errorSink.OnError(
                    documentLocation,
                    Resources.FormatTagHelperTypeResolver_CannotResolveTagHelperAssembly(
                        assemblyName.Name,
                        ex.Message),
                    name.Length);

                return Enumerable.Empty<ITypeInfo>();
            }

            return libraryTypes.Where(IsTagHelper);
        }

        /// <summary>
        /// Returns all non-nested exported types from the given <paramref name="assemblyName"/>
        /// </summary>
        /// <param name="assemblyName">The <see cref="AssemblyName"/> to get <see cref="ITypeInfo"/>s from.</param>
        /// <returns>
        /// An <see cref="IEnumerable{ITypeInfo}"/> of types exported from the given <paramref name="assemblyName"/>.
        /// </returns>
        protected virtual IEnumerable<ITypeInfo> GetTopLevelExportedTypes(AssemblyName assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            var exportedTypeInfos = GetExportedTypes(assemblyName);

            return exportedTypeInfos
                .Where(typeInfo => !typeInfo.IsNested)
                .Select(typeInfo => new RuntimeTypeInfo(typeInfo));
        }

        /// <summary>
        /// Returns all exported types from the given <paramref name="assemblyName"/>
        /// </summary>
        /// <param name="assemblyName">The <see cref="AssemblyName"/> to get <see cref="TypeInfo"/>s from.</param>
        /// <returns>
        /// An <see cref="IEnumerable{TypeInfo}"/> of types exported from the given <paramref name="assemblyName"/>.
        /// </returns>
        protected virtual IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);

            return assembly.ExportedTypes.Select(type => type.GetTypeInfo());
        }

        // Internal for testing.
        internal virtual bool IsTagHelper(ITypeInfo typeInfo)
        {
            return typeInfo.IsPublic &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.IsGenericType &&
                   typeInfo.ImplementsInterface(ITagHelperTypeInfo);
        }
    }
}