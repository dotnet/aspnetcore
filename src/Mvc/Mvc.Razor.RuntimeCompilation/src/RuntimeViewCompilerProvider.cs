// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

internal sealed class RuntimeViewCompilerProvider : IViewCompilerProvider
{
    private readonly RazorProjectEngine _razorProjectEngine;
    private readonly ApplicationPartManager _applicationPartManager;
    private readonly CSharpCompiler _csharpCompiler;
    private readonly RuntimeCompilationFileProvider _fileProvider;
    private readonly ILogger<RuntimeViewCompiler> _logger;
    private readonly Func<IViewCompiler> _createCompiler;

    private object _initializeLock = new object();
    private bool _initialized;
    private IViewCompiler? _compiler;

    public RuntimeViewCompilerProvider(
        ApplicationPartManager applicationPartManager,
        RazorProjectEngine razorProjectEngine,
        RuntimeCompilationFileProvider fileProvider,
        CSharpCompiler csharpCompiler,
        ILoggerFactory loggerFactory)
    {
        _applicationPartManager = applicationPartManager;
        _razorProjectEngine = razorProjectEngine;
        _csharpCompiler = csharpCompiler;
        _fileProvider = fileProvider;

        _logger = loggerFactory.CreateLogger<RuntimeViewCompiler>();
        _createCompiler = CreateCompiler;
    }

    public IViewCompiler GetCompiler()
    {
        return LazyInitializer.EnsureInitialized(
            ref _compiler,
            ref _initialized,
            ref _initializeLock,
            _createCompiler)!;
    }

    private IViewCompiler CreateCompiler()
    {
        var feature = new ViewsFeature();
        _applicationPartManager.PopulateFeature(feature);

        return new RuntimeViewCompiler(
            _fileProvider.FileProvider,
            _razorProjectEngine,
            _csharpCompiler,
            feature.ViewDescriptors,
            _logger);
    }
}
