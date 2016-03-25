// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class that locates valid <see cref="ITagHelper"/>s within an assembly.
    /// </summary>
    public class TagHelperTypeResolver : ITagHelperTypeResolver
    {
        private static readonly TypeInfo ITagHelperTypeInfo = typeof(ITagHelper).GetTypeInfo();

        /// <inheritdoc />
        public IEnumerable<Type> Resolve(
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

                return Type.EmptyTypes;
            }

            var assemblyName = new AssemblyName(name);

            IEnumerable<TypeInfo> libraryTypes;
            try
            {
                libraryTypes = GetExportedTypes(assemblyName);
            }
            catch (Exception ex)
            {
                errorSink.OnError(
                    documentLocation,
                    Resources.FormatTagHelperTypeResolver_CannotResolveTagHelperAssembly(
                        assemblyName.Name,
                        ex.Message),
                    name.Length);

                return Type.EmptyTypes;
            }

            return libraryTypes.Where(IsTagHelper).Select(t => t.AsType());
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

        /// <summary>
        /// Indicates if a <see cref="TypeInfo"/> should be treated as a tag helper.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/> to inspect.</param>
        /// <returns><c>true</c> if <paramref name="typeInfo"/> should be treated as a tag helper; 
        /// <c>false</c> otherwise</returns>
        protected virtual bool IsTagHelper(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return TagHelperConventions.IsTagHelper(typeInfo);
        }
    }
}