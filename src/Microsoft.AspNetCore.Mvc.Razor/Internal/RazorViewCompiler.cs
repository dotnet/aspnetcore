// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Caches the result of runtime compilation of Razor files for the duration of the application lifetime.
    /// </summary>
    public class RazorViewCompiler : IViewCompiler
    {
        private readonly object _cacheLock = new object();
        private readonly Dictionary<string, Task<CompiledViewDescriptor>> _precompiledViewLookup;
        private readonly ConcurrentDictionary<string, string> _normalizedPathLookup;
        private readonly IFileProvider _fileProvider;
        private readonly RazorTemplateEngine _templateEngine;
        private readonly Action<RoslynCompilationContext> _compilationCallback;
        private readonly ILogger _logger;
        private readonly CSharpCompiler _csharpCompiler;
        private readonly IMemoryCache _cache;

        public RazorViewCompiler(
            IFileProvider fileProvider,
            RazorTemplateEngine templateEngine,
            CSharpCompiler csharpCompiler,
            Action<RoslynCompilationContext> compilationCallback,
            IList<CompiledViewDescriptor> precompiledViews,
            ILogger logger)
        {
            if (fileProvider == null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            if (templateEngine == null)
            {
                throw new ArgumentNullException(nameof(templateEngine));
            }

            if (csharpCompiler == null)
            {
                throw new ArgumentNullException(nameof(csharpCompiler));
            }

            if (compilationCallback == null)
            {
                throw new ArgumentNullException(nameof(compilationCallback));
            }

            if (precompiledViews == null)
            {
                throw new ArgumentNullException(nameof(precompiledViews));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _fileProvider = fileProvider;
            _templateEngine = templateEngine;
            _csharpCompiler = csharpCompiler;
            _compilationCallback = compilationCallback;
            _logger = logger;

            _normalizedPathLookup = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            _cache = new MemoryCache(new MemoryCacheOptions());

            _precompiledViewLookup = new Dictionary<string, Task<CompiledViewDescriptor>>(
                precompiledViews.Count,
                StringComparer.OrdinalIgnoreCase);

            foreach (var precompiledView in precompiledViews)
            {
                if (_precompiledViewLookup.TryGetValue(precompiledView.RelativePath, out var otherValue))
                {
                    var message = string.Join(
                        Environment.NewLine,
                        Resources.RazorViewCompiler_ViewPathsDifferOnlyInCase,
                        otherValue.Result.RelativePath,
                        precompiledView.RelativePath);
                    throw new InvalidOperationException(message);
                }

                _precompiledViewLookup.Add(precompiledView.RelativePath, Task.FromResult(precompiledView));
            }
        }

        /// <inheritdoc />
        public Task<CompiledViewDescriptor> CompileAsync(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            // Lookup precompiled views first.

            // Attempt to lookup the cache entry using the passed in path. This will succeed if the path is already
            // normalized and a cache entry exists.
            string normalizedPath = null;
            Task<CompiledViewDescriptor> cachedResult;

            if (_precompiledViewLookup.Count > 0)
            {
                if (_precompiledViewLookup.TryGetValue(relativePath, out cachedResult))
                {
                    return cachedResult;
                }

                normalizedPath = GetNormalizedPath(relativePath);
                if (_precompiledViewLookup.TryGetValue(normalizedPath, out cachedResult))
                {
                    return cachedResult;
                }
            }

            if (_cache.TryGetValue(relativePath, out cachedResult))
            {
                return cachedResult;
            }

            normalizedPath = normalizedPath ?? GetNormalizedPath(relativePath);
            if (_cache.TryGetValue(normalizedPath, out cachedResult))
            {
                return cachedResult;
            }

            // Entry does not exist. Attempt to create one.
            cachedResult = CreateCacheEntry(normalizedPath);
            return cachedResult;
        }

        private Task<CompiledViewDescriptor> CreateCacheEntry(string normalizedPath)
        {
            TaskCompletionSource<CompiledViewDescriptor> compilationTaskSource = null;
            MemoryCacheEntryOptions cacheEntryOptions;
            Task<CompiledViewDescriptor> cacheEntry;

            // Safe races cannot be allowed when compiling Razor pages. To ensure only one compilation request succeeds
            // per file, we'll lock the creation of a cache entry. Creating the cache entry should be very quick. The
            // actual work for compiling files happens outside the critical section.
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(normalizedPath, out cacheEntry))
                {
                    return cacheEntry;
                }

                cacheEntryOptions = new MemoryCacheEntryOptions();

                cacheEntryOptions.ExpirationTokens.Add(_fileProvider.Watch(normalizedPath));
                var projectItem = _templateEngine.Project.GetItem(normalizedPath);
                if (!projectItem.Exists)
                {
                    cacheEntry = Task.FromResult(new CompiledViewDescriptor
                    {
                        RelativePath = normalizedPath,
                        ExpirationTokens = cacheEntryOptions.ExpirationTokens,
                    });
                }
                else
                {
                    // A file exists and needs to be compiled.
                    compilationTaskSource = new TaskCompletionSource<CompiledViewDescriptor>();
                    foreach (var importItem in _templateEngine.GetImportItems(projectItem))
                    {
                        cacheEntryOptions.ExpirationTokens.Add(_fileProvider.Watch(importItem.FilePath));
                    }
                    cacheEntry = compilationTaskSource.Task;
                }

                cacheEntry = _cache.Set(normalizedPath, cacheEntry, cacheEntryOptions);
            }

            if (compilationTaskSource != null)
            {
                // Indicates that a file was found and needs to be compiled.
                Debug.Assert(cacheEntryOptions != null);

                try
                {
                    var descriptor = CompileAndEmit(normalizedPath);
                    descriptor.ExpirationTokens = cacheEntryOptions.ExpirationTokens;
                    compilationTaskSource.SetResult(descriptor);
                }
                catch (Exception ex)
                {
                    compilationTaskSource.SetException(ex);
                }
            }

            return cacheEntry;
        }

        protected virtual CompiledViewDescriptor CompileAndEmit(string relativePath)
        {
            var codeDocument = _templateEngine.CreateCodeDocument(relativePath);
            var cSharpDocument = _templateEngine.GenerateCode(codeDocument);

            if (cSharpDocument.Diagnostics.Count > 0)
            {
                throw CompilationFailedExceptionFactory.Create(
                    codeDocument,
                    cSharpDocument.Diagnostics);
            }

            var generatedAssembly = CompileAndEmit(codeDocument, cSharpDocument.GeneratedCode);
            var viewAttribute = generatedAssembly.GetCustomAttribute<RazorViewAttribute>();
            return new CompiledViewDescriptor
            {
                ViewAttribute = viewAttribute,
                RelativePath = relativePath,
            };
        }

        internal Assembly CompileAndEmit(RazorCodeDocument codeDocument, string generatedCode)
        {
            _logger.GeneratedCodeToAssemblyCompilationStart(codeDocument.Source.FilePath);

            var startTimestamp = _logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;

            var assemblyName = Path.GetRandomFileName();
            var compilation = CreateCompilation(generatedCode, assemblyName);

            using (var assemblyStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var result = compilation.Emit(
                    assemblyStream,
                    pdbStream,
                    options: _csharpCompiler.EmitOptions);

                if (!result.Success)
                {
                    throw CompilationFailedExceptionFactory.Create(
                        codeDocument,
                        generatedCode,
                        assemblyName,
                        result.Diagnostics);
                }

                assemblyStream.Seek(0, SeekOrigin.Begin);
                pdbStream.Seek(0, SeekOrigin.Begin);

                var assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
                _logger.GeneratedCodeToAssemblyCompilationEnd(codeDocument.Source.FilePath, startTimestamp);

                return assembly;
            }
        }

        private CSharpCompilation CreateCompilation(string compilationContent, string assemblyName)
        {
            var sourceText = SourceText.From(compilationContent, Encoding.UTF8);
            var syntaxTree = _csharpCompiler.CreateSyntaxTree(sourceText).WithFilePath(assemblyName);
            var compilation = _csharpCompiler
                .CreateCompilation(assemblyName)
                .AddSyntaxTrees(syntaxTree);
            compilation = ExpressionRewriter.Rewrite(compilation);

            var compilationContext = new RoslynCompilationContext(compilation);
            _compilationCallback(compilationContext);
            compilation = compilationContext.Compilation;
            return compilation;
        }

        private string GetNormalizedPath(string relativePath)
        {
            Debug.Assert(relativePath != null);
            if (relativePath.Length == 0)
            {
                return relativePath;
            }

            if (!_normalizedPathLookup.TryGetValue(relativePath, out var normalizedPath))
            {
                normalizedPath = ViewPath.NormalizePath(relativePath);
                _normalizedPathLookup[relativePath] = normalizedPath;
            }

            return normalizedPath;
        }
    }
}
