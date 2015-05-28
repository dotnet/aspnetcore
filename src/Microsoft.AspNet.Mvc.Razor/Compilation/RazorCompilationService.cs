// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// Default implementation of <see cref="IRazorCompilationService"/>.
    /// </summary>
    public class RazorCompilationService : IRazorCompilationService
    {
        private readonly ICompilationService _compilationService;
        private readonly IMvcRazorHost _razorHost;
        private readonly IFileProvider _fileProvider;

        public RazorCompilationService(ICompilationService compilationService,
                                       IMvcRazorHost razorHost,
                                       IOptions<RazorViewEngineOptions> viewEngineOptions)
        {
            _compilationService = compilationService;
            _razorHost = razorHost;
            _fileProvider = viewEngineOptions.Options.FileProvider;
        }

        /// <inheritdoc />
        public CompilationResult Compile([NotNull] RelativeFileInfo file)
        {
            GeneratorResults results;
            using (var inputStream = file.FileInfo.CreateReadStream())
            {
                results = _razorHost.GenerateCode(file.RelativePath, inputStream);
            }

            if (!results.Success)
            {
                return GetCompilationFailedResult(file, results.ParserErrors);
            }

            return _compilationService.Compile(file, results.GeneratedCode);
        }

        // Internal for unit testing
        internal CompilationResult GetCompilationFailedResult(RelativeFileInfo file, IEnumerable<RazorError> errors)
        {
            // If a SourceLocation does not specify a file path, assume it is produced
            // from parsing the current file.
            var messageGroups = errors
                .GroupBy(razorError =>
                razorError.Location.FilePath ?? file.RelativePath,
                StringComparer.Ordinal);

            var failures = new List<RazorCompilationFailure>();
            foreach (var group in messageGroups)
            {
                var filePath = group.Key;
                var fileContent = ReadFileContentsSafely(filePath);
                var compilationFailure = new RazorCompilationFailure(
                    filePath,
                    fileContent,
                    group.Select(parserError => new RazorCompilationMessage(parserError, filePath)));
                failures.Add(compilationFailure);
            }

            return CompilationResult.Failed(failures);
        }

        private string ReadFileContentsSafely(string relativePath)
        {
            var fileInfo = _fileProvider.GetFileInfo(relativePath);
            if (fileInfo.Exists)
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
                }
            }

            return null;
        }
    }
}
