// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Razor;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorCompilationService : IRazorCompilationService
    {
        // This class must be registered as a singleton service for the caching to work.
        private readonly CompilerCache _cache;
        private readonly ICompilationService _baseCompilationService;
        private readonly IMvcRazorHost _razorHost;

        public RazorCompilationService(ICompilationService compilationService,
                                       IControllerAssemblyProvider _controllerAssemblyProvider,
                                       IMvcRazorHost razorHost)
        {
            _baseCompilationService = compilationService;
            _razorHost = razorHost;
            _cache = new CompilerCache(_controllerAssemblyProvider.CandidateAssemblies);
        }

        public CompilationResult Compile([NotNull] RelativeFileInfo file)
        {
            return _cache.GetOrAdd(file, () => CompileCore(file));
        }

        internal CompilationResult CompileCore(RelativeFileInfo file)
        {
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

            return _baseCompilationService.Compile(file.FileInfo, results.GeneratedCode);
        }
    }
}
