// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Mvc.Razor.Precompilation
{
    /// <summary>
    /// Specifies metadata about precompiled views.
    /// </summary>
    public abstract class RazorFileInfoCollection
    {
        /// <summary>
        /// Gets or sets the name of the resource containing the precompiled binary.
        /// </summary>
        public string AssemblyResourceName { get; protected set; }

        /// <summary>
        /// Gets or sets the name of the resource that contains the symbols (pdb).
        /// </summary>
        public string SymbolsResourceName { get; protected set; }

        /// <summary>
        /// Gets the <see cref="IReadOnlyList{T}"/> of <see cref="RazorFileInfo"/>s.
        /// </summary>
        public IReadOnlyList<RazorFileInfo> FileInfos { get; protected set; }

        /// <summary>
        /// Loads the assembly containing precompiled views. 
        /// </summary>
        /// <param name="loadContext">The <see cref="IAssemblyLoadContext"/>.</param>
        /// <returns>The <see cref="Assembly"/> containing precompiled views.</returns>
        public virtual Assembly LoadAssembly(IAssemblyLoadContext loadContext)
        {
            var viewCollectionAssembly = GetType().GetTypeInfo().Assembly;

            using (var assemblyStream = viewCollectionAssembly.GetManifestResourceStream(AssemblyResourceName))
            {
                if (assemblyStream == null)
                {
                    var message = Resources.FormatRazorFileInfoCollection_ResourceCouldNotBeFound(AssemblyResourceName,
                                                                                                  GetType().FullName);
                    throw new InvalidOperationException(message);
                }

                Stream symbolsStream = null;
                if (!string.IsNullOrEmpty(SymbolsResourceName))
                {
                    symbolsStream = viewCollectionAssembly.GetManifestResourceStream(SymbolsResourceName);
                }

                using (symbolsStream)
                {
                    return loadContext.LoadStream(assemblyStream, symbolsStream);
                }
            }
        }
    }
}