// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation of <see cref="IRazorCompilationService"/>.
    /// </summary>
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
        public CompilationResult Compile([NotNull] RelativeFileInfo file)
        {
            GeneratorResults results;
            using (var inputStream = file.FileInfo.CreateReadStream())
            {
                results = _razorHost.GenerateCode(
                    file.RelativePath, inputStream);
            }

            if (!results.Success)
            {
                var messages = results.ParserErrors
                                      .Select(parseError => new RazorCompilationMessage(parseError, file.RelativePath));
                var failure = new RazorCompilationFailure(
                    file.RelativePath,
                    ReadFileContentsSafely(file.FileInfo),
                    messages);

                return CompilationResult.Failed(failure);
            }

            return _compilationService.Compile(file, results.GeneratedCode);
        }

        private static string ReadFileContentsSafely(IFileInfo fileInfo)
        {
            try
            {
                using (var reader = new StreamReader(fileInfo.CreateReadStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                // Ignore any failures
                return null;
            }
        }
    }
}
