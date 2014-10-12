// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Razor;

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
        private readonly ICompilationService _compilationService;
        private readonly IMvcRazorHost _razorHost;

        public RazorCompilationService(ICompilationService compilationService,
                                       IMvcRazorHost razorHost)
        {
            _compilationService = compilationService;
            _razorHost = razorHost;
        }

        /// <inheritdoc />
        public CompilationResult Compile([NotNull] RelativeFileInfo file, bool isInstrumented)
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

            return _compilationService.Compile(file.FileInfo, results.GeneratedCode);
        }
    }
}
