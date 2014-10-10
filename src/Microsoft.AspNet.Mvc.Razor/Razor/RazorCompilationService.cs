// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Razor;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation of <see cref="IRazorCompilationService"/>.
    /// </summary>
    /// <remarks>
    /// This class must be registered as a singleton service for the caching to work.
    /// </remarks>
    public class RazorCompilationService : IRazorCompilationService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly CompilerCache _cache;
        private ICompilationService _compilationService;

        private ICompilationService CompilationService
        {
            get
            {
                if (_compilationService == null)
                {
                    _compilationService = _serviceProvider.GetService<ICompilationService>();
                }

                return _compilationService;
            }
        }

        private readonly IMvcRazorHost _razorHost;

        public RazorCompilationService(IServiceProvider serviceProvider,
                                       IControllerAssemblyProvider _controllerAssemblyProvider,
                                       IMvcRazorHost razorHost)
        {
            _serviceProvider = serviceProvider;
            _razorHost = razorHost;
            _cache = new CompilerCache(_controllerAssemblyProvider.CandidateAssemblies);
        }

        /// <inheritdoc />
        public CompilationResult Compile([NotNull] RelativeFileInfo file, bool isInstrumented)
        {
            return _cache.GetOrAdd(file, isInstrumented, () => CompileCore(file, isInstrumented));
        }

        internal CompilationResult CompileCore(RelativeFileInfo file, bool isInstrumented)
        {
            _razorHost.EnableInstrumentation = isInstrumented;

            GeneratorResults results;
            using (var inputStream = file.FileInfo.CreateReadStream())
            {
                results = _razorHost.GenerateCode(
                    file.RelativePath, inputStream);
            }

            if (!results.Success)
            {
                var messages = results.ParserErrors.Select(e => new CompilationMessage(e.Message));
                return CompilationResult.Failed(file.FileInfo, results.GeneratedCode, messages);
            }

            return CompilationService.Compile(file.FileInfo, results.GeneratedCode);
        }
    }
}
