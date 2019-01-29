// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    public class MvcRazorRuntimeCompilationOptions
    {
        /// <summary>
        /// Gets the <see cref="IFileProvider" /> instances used to locate Razor files.
        /// </summary>
        /// <remarks>
        /// At startup, this collection is initialized to include an instance of
        /// <see cref="IHostingEnvironment.ContentRootFileProvider"/> that is rooted at the application root.
        /// </remarks>
        public IList<IFileProvider> FileProviders { get; } = new List<IFileProvider>();

        /// <summary>
        /// Gets paths to additional references used during runtime compilation of Razor files.
        /// </summary>
        /// <remarks>
        /// By default, the runtime compiler <see cref="ICompilationReferencesProvider"/> to gather references
        /// uses to compile a Razor file. This API allows providing additional references to the compiler.
        /// </remarks>
        public IList<string> AdditionalReferencePaths { get; } = new List<string>();
    }
}
