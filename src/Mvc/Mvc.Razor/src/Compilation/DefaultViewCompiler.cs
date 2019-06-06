// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Caches the result of runtime compilation of Razor files for the duration of the application lifetime.
    /// </summary>
    internal class DefaultViewCompiler : IViewCompiler
    {
        private readonly Dictionary<string, Task<CompiledViewDescriptor>> _compiledViews;
        private readonly ConcurrentDictionary<string, string> _normalizedPathCache;
        private readonly ILogger _logger;

        public DefaultViewCompiler(
            IList<CompiledViewDescriptor> compiledViews,
            ILogger logger)
        {
            if (compiledViews == null)
            {
                throw new ArgumentNullException(nameof(compiledViews));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
            _normalizedPathCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

            // We need to validate that the all of the precompiled views are unique by path (case-insensitive).
            // We do this because there's no good way to canonicalize paths on windows, and it will create
            // problems when deploying to linux. Rather than deal with these issues, we just don't support
            // views that differ only by case.
            _compiledViews = new Dictionary<string, Task<CompiledViewDescriptor>>(
                compiledViews.Count,
                StringComparer.OrdinalIgnoreCase);

            foreach (var compiledView in compiledViews)
            {
                logger.ViewCompilerLocatedCompiledView(compiledView.RelativePath);

                if (!_compiledViews.ContainsKey(compiledView.RelativePath))
                {
                    // View ordering has precedence semantics, a view with a higher precedence was not
                    // already added to the list.
                    _compiledViews.Add(compiledView.RelativePath, Task.FromResult(compiledView));
                }
            }

            if (_compiledViews.Count == 0)
            {
                logger.ViewCompilerNoCompiledViewsFound();
            }
        }

        /// <inheritdoc />
        public Task<CompiledViewDescriptor> CompileAsync(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            // Attempt to lookup the cache entry using the passed in path. This will succeed if the path is already
            // normalized and a cache entry exists.
            if (_compiledViews.TryGetValue(relativePath, out var cachedResult))
            {
                _logger.ViewCompilerLocatedCompiledViewForPath(relativePath);
                return cachedResult;
            }

            var normalizedPath = GetNormalizedPath(relativePath);
            if (_compiledViews.TryGetValue(normalizedPath, out cachedResult))
            {
                _logger.ViewCompilerLocatedCompiledViewForPath(normalizedPath);
                return cachedResult;
            }

            // Entry does not exist. Attempt to create one.
            _logger.ViewCompilerCouldNotFindFileAtPath(relativePath);
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
    }
}
