// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
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
                                      .Select(parseError =>
                                        new CompilationMessage(parseError.Message,
                                                               parseError.Location.CharacterIndex,
                                                               parseError.Location.LineIndex,
                                                               parseError.Location.CharacterIndex + parseError.Length,
                                                               parseError.Location.LineIndex));
                return CompilationResult.Failed(file.FileInfo, results.GeneratedCode, messages);
            }

            return _compilationService.Compile(file.FileInfo, results.GeneratedCode);
        }
    }
}
