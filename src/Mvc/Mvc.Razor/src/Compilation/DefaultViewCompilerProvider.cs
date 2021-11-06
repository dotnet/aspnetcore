// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

// This type is referenced by name by the RuntimeCompilation package. Do not rename it
internal sealed class DefaultViewCompilerProvider : IViewCompilerProvider
{
    private readonly DefaultViewCompiler _compiler;

    public DefaultViewCompilerProvider(
        ApplicationPartManager applicationPartManager,
        ILoggerFactory loggerFactory)
    {
        _compiler = new DefaultViewCompiler(applicationPartManager, loggerFactory.CreateLogger<DefaultViewCompiler>());
    }

    internal DefaultViewCompiler Compiler => _compiler;

    public IViewCompiler GetCompiler() => _compiler;
}
