// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

/// <summary>
/// Used to configure razor compilation.
/// </summary>
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
