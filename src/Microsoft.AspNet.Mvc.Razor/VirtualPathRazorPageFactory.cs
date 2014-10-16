// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a <see cref="IRazorPageFactory"/> that creates <see cref="RazorPage"/> instances
    /// from razor files in the file system.
    /// </summary>
    public class VirtualPathRazorPageFactory : IRazorPageFactory
    {
        private readonly ITypeActivator _activator;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileInfoCache _fileInfoCache;
        private readonly ICompilerCache _compilerCache;
        private readonly bool _isInstrumentationEnabled;
        private IRazorCompilationService _razorcompilationService;

        public VirtualPathRazorPageFactory(ITypeActivator typeActivator,
                                           IServiceProvider serviceProvider,
                                           ICompilerCache compilerCache,
                                           IFileInfoCache fileInfoCache,
                                           IContextAccessor<HttpContext> contextAccessor)
        {
            _activator = typeActivator;
            _serviceProvider = serviceProvider;
            _compilerCache = compilerCache;
            _fileInfoCache = fileInfoCache;
            _isInstrumentationEnabled = IsInstrumentationEnabled(contextAccessor.Value);
        }

        private IRazorCompilationService RazorCompilationService
        {
            get
            {
                if (_razorcompilationService == null)
                {
                    // it is ok to use the cached service provider because both this, and the
                    // resolved service are in a lifetime of Scoped.
                    // We don't want to get it upgront because it will force Roslyn to load.
                    _razorcompilationService = _serviceProvider.GetRequiredService<IRazorCompilationService>();
                }

                return _razorcompilationService;
            }
        }

        /// <inheritdoc />
        public IRazorPage CreateInstance([NotNull] string relativePath)
        {
            if (relativePath.StartsWith("~/", StringComparison.Ordinal))
            {
                // For tilde slash paths, drop the leading ~ to make it work with the underlying IFileSystem.
                relativePath = relativePath.Substring(1);
            }

            var fileInfo = _fileInfoCache.GetFileInfo(relativePath);

            if (fileInfo != null)
            {
                var relativeFileInfo = new RelativeFileInfo()
                {
                    FileInfo = fileInfo,
                    RelativePath = relativePath,
                };

                var result = _compilerCache.GetOrAdd(
                    relativeFileInfo,
                    _isInstrumentationEnabled,
                    () => RazorCompilationService.Compile(relativeFileInfo, _isInstrumentationEnabled));

                var page = (IRazorPage)_activator.CreateInstance(_serviceProvider, result.CompiledType);
                page.Path = relativePath;

                return page;
            }

            return null;
        }

        private static bool IsInstrumentationEnabled(HttpContext context)
        {
            return context.GetFeature<IPageExecutionListenerFeature>() != null;
        }
    }
}
