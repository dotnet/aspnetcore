// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    /// <summary>
    /// <see cref="TagHelperTypeResolver"/> used during Razor precompilation.
    /// </summary>
    public class PrecompilationTagHelperTypeResolver : TagHelperTypeResolver
    {
        private readonly object _assemblyLookupLock = new object();
        private readonly Dictionary<string, IEnumerable<ITypeInfo>> _assemblyLookup
            = new Dictionary<string, IEnumerable<ITypeInfo>>(StringComparer.Ordinal);
        private readonly Compilation _compilation;

        /// <summary>
        /// Initializes a new instance of <see cref="PrecompilationTagHelperTypeResolver"/>.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/>.</param>
        public PrecompilationTagHelperTypeResolver([NotNull] Compilation compilation)
        {
            _compilation = compilation;
        }

        /// <inheritdoc />
        protected override IEnumerable<ITypeInfo> GetTopLevelExportedTypes([NotNull] AssemblyName assemblyName)
        {
            lock (_assemblyLookupLock)
            {
                IEnumerable<ITypeInfo> result;
                if (!_assemblyLookup.TryGetValue(assemblyName.Name, out result))
                {
                    result = GetExportedTypes(assemblyName.Name);
                    _assemblyLookup[assemblyName.Name] = result;
                }

                return result;
            }
        }

        // Internal for unit testing
        internal IEnumerable<ITypeInfo> GetExportedTypes(string assemblyName)
        {
            if (string.Equals(_compilation.AssemblyName, assemblyName, StringComparison.Ordinal))
            {
                return GetExportedTypes(_compilation.Assembly);
            }
            else
            {
                foreach (var reference in _compilation.References)
                {
                    var compilationReference = reference as CompilationReference;
                    if (compilationReference != null &&
                        string.Equals(
                            compilationReference.Compilation.AssemblyName,
                            assemblyName,
                            StringComparison.Ordinal))
                    {
                        return GetExportedTypes(compilationReference.Compilation.Assembly);
                    }

                    var assemblySymbol = _compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                    if (string.Equals(
                        assemblySymbol?.Identity.Name,
                        assemblyName,
                        StringComparison.Ordinal))
                    {
                        return GetExportedTypes(assemblySymbol);
                    }
                }
            }

            throw new InvalidOperationException(
                Resources.FormatCodeAnalysis_UnableToLoadAssemblyReference(assemblyName));
        }

        private static List<ITypeInfo> GetExportedTypes(IAssemblySymbol assembly)
        {
            var exportedTypes = new List<ITypeInfo>();
            GetExportedTypes(assembly.GlobalNamespace, exportedTypes);
            return exportedTypes;
        }

        private static void GetExportedTypes(INamespaceSymbol namespaceSymbol, List<ITypeInfo> exportedTypes)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                if (type.TypeKind == TypeKind.Class &&
                    type.DeclaredAccessibility == Accessibility.Public)
                {
                    exportedTypes.Add(new CodeAnalysisSymbolBasedTypeInfo(type));
                }
            }

            foreach (var subNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                GetExportedTypes(subNamespace, exportedTypes);
            }
        }
    }
}