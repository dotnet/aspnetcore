// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

/// <summary>
/// Caches the result of runtime compilation of Razor files for the duration of the application lifetime.
/// </summary>
#pragma warning disable CA1852 // Seal internal types
// This name is hardcoded in RazorRuntimeCompilationMvcCoreBuilderExtensions. Make sure it's updated if this is ever renamed.
internal partial class DefaultViewCompiler : IViewCompiler
#pragma warning restore CA1852 // Seal internal types
{
    private readonly ApplicationPartManager _applicationPartManager;
    private readonly ConcurrentDictionary<string, string> _normalizedPathCache;
    private Dictionary<string, Task<CompiledViewDescriptor>>? _compiledViews;
    private readonly ILogger _logger;

    public DefaultViewCompiler(
        ApplicationPartManager applicationPartManager,
        ILogger<DefaultViewCompiler> logger)
    {
        _applicationPartManager = applicationPartManager;
        _logger = logger;
        _normalizedPathCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        EnsureCompiledViews(logger);
    }

    [MemberNotNull(nameof(_compiledViews))]
    private void EnsureCompiledViews(ILogger logger)
    {
        if (_compiledViews is not null)
        {
            return;
        }

        var viewsFeature = new ViewsFeature();
        _applicationPartManager.PopulateFeature(viewsFeature);

        // We need to validate that the all compiled views are unique by path (case-insensitive).
        // We do this because there's no good way to canonicalize paths on windows, and it will create
        // problems when deploying to linux. Rather than deal with these issues, we just don't support
        // views that differ only by case.
        var compiledViews = new Dictionary<string, Task<CompiledViewDescriptor>>(
            viewsFeature.ViewDescriptors.Count,
            StringComparer.OrdinalIgnoreCase);

        foreach (var compiledView in viewsFeature.ViewDescriptors)
        {
            Log.ViewCompilerLocatedCompiledView(logger, compiledView.RelativePath);

            if (!compiledViews.ContainsKey(compiledView.RelativePath))
            {
                // View ordering has precedence semantics, a view with a higher precedence was not
                // already added to the list.
                compiledViews.TryAdd(compiledView.RelativePath, Task.FromResult(compiledView));
            }
        }

        if (compiledViews.Count == 0)
        {
            Log.ViewCompilerNoCompiledViewsFound(logger);
        }

        // Safe races should be ok. We would end up logging multiple times
        // if this is invoked concurrently, but since this is primarily a dev-scenario, we don't think
        // this will happen often. We could always re-consider the logging if we get feedback.
        _compiledViews = compiledViews;
    }

    internal Dictionary<string, Task<CompiledViewDescriptor>>? CompiledViews => _compiledViews;

    // Invoked as part of a hot reload event.
    internal void ClearCache()
    {
        _compiledViews = null;
    }

    /// <inheritdoc />
    public Task<CompiledViewDescriptor> CompileAsync(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        EnsureCompiledViews(_logger);

        // Attempt to lookup the cache entry using the passed in path. This will succeed if the path is already
        // normalized and a cache entry exists.
        if (_compiledViews.TryGetValue(relativePath, out var cachedResult))
        {
            Log.ViewCompilerLocatedCompiledViewForPath(_logger, relativePath);
            return cachedResult;
        }

        var normalizedPath = GetNormalizedPath(relativePath);
        if (_compiledViews.TryGetValue(normalizedPath, out cachedResult))
        {
            Log.ViewCompilerLocatedCompiledViewForPath(_logger, normalizedPath);
            return cachedResult;
        }

        // Entry does not exist. Attempt to create one.
        Log.ViewCompilerCouldNotFindFileAtPath(_logger, relativePath);
        return Task.FromResult(new CompiledViewDescriptor
        {
            RelativePath = normalizedPath,
            ExpirationTokens = Array.Empty<IChangeToken>(),
        });
    }

    private string GetNormalizedPath(string relativePath)
    {
        Debug.Assert(relativePath != null);
        if (relativePath.Length == 0)
        {
            return relativePath;
        }

        if (!_normalizedPathCache.TryGetValue(relativePath, out var normalizedPath))
        {
            normalizedPath = ViewPath.NormalizePath(relativePath);
            _normalizedPathCache[relativePath] = normalizedPath;
        }

        return normalizedPath;
    }

    private static partial class Log
    {
        [LoggerMessage(3, LogLevel.Debug, "Initializing Razor view compiler with compiled view: '{ViewName}'.", EventName = "ViewCompilerLocatedCompiledView")]
        public static partial void ViewCompilerLocatedCompiledView(ILogger logger, string viewName);

        [LoggerMessage(4, LogLevel.Debug, "Initializing Razor view compiler with no compiled views.", EventName = "ViewCompilerNoCompiledViewsFound")]
        public static partial void ViewCompilerNoCompiledViewsFound(ILogger logger);

        [LoggerMessage(5, LogLevel.Trace, "Located compiled view for view at path '{Path}'.", EventName = "ViewCompilerLocatedCompiledViewForPath")]
        public static partial void ViewCompilerLocatedCompiledViewForPath(ILogger logger, string path);

        [LoggerMessage(7, LogLevel.Trace, "Could not find a file for view at path '{Path}'.", EventName = "ViewCompilerCouldNotFindFileAtPath")]
        public static partial void ViewCompilerCouldNotFindFileAtPath(ILogger logger, string path);
    }
}
