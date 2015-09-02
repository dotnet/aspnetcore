// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Runtime;
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

        /// <summary>
        /// Instantiates a new instance of the <see cref="RazorCompilationService"/> class.
        /// </summary>
        /// <param name="compilationService">The <see cref="ICompilationService"/> to compile generated code.</param>
        /// <param name="razorHost">The <see cref="IMvcRazorHost"/> to generate code from Razor files.</param>
        /// <param name="viewEngineOptions">
        /// The <see cref="IFileProvider"/> to read Razor files referenced in error messages.
        /// </param>
        public RazorCompilationService(
            ICompilationService compilationService,
            IMvcRazorHost razorHost,
            IOptions<RazorViewEngineOptions> viewEngineOptions)
        {
            _compilationService = compilationService;
            _razorHost = razorHost;
            _fileProvider = viewEngineOptions.Value.FileProvider;
        }

        /// <inheritdoc />
        public CompilationResult Compile([NotNull] RelativeFileInfo file)
        {
            GeneratorResults results;
            using (var inputStream = file.FileInfo.CreateReadStream())
            {
                results = GenerateCode(file.RelativePath, inputStream);
            }

            if (!results.Success)
            {
                return GetCompilationFailedResult(file, results.ParserErrors);
            }

            return _compilationService.Compile(file, results.GeneratedCode);
        }

        /// <summary>
        /// Generate code for the Razor file at <paramref name="relativePath"/> with content
        /// <paramref name="inputStream"/>.
        /// </summary>
        /// <param name="relativePath">
        /// The path of the Razor file relative to the root of the application. Used to generate line pragmas and
        /// calculate the class name of the generated type.
        /// </param>
        /// <param name="inputStream">A <see cref="Stream"/> that contains the Razor content.</param>
        /// <returns>A <see cref="GeneratorResults"/> instance containing results of code generation.</returns>
        protected virtual GeneratorResults GenerateCode(string relativePath, Stream inputStream)
        {
            return _razorHost.GenerateCode(relativePath, inputStream);
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

            var failures = new List<CompilationFailure>();
            foreach (var group in messageGroups)
            {
                var filePath = group.Key;
                var fileContent = ReadFileContentsSafely(filePath);
                var compilationFailure = new CompilationFailure(
                    filePath,
                    fileContent,
                    group.Select(parserError => CreateDiagnosticMessage(parserError, filePath)));
                failures.Add(compilationFailure);
            }

            return CompilationResult.Failed(failures);
        }

        private DiagnosticMessage CreateDiagnosticMessage(RazorError error, string filePath)
        {
            return new DiagnosticMessage(
                error.Message,
                $"{error} ({error.Location.LineIndex},{error.Location.CharacterIndex}) {error.Message}",
                filePath,
                DiagnosticMessageSeverity.Error,
                error.Location.LineIndex + 1,
                error.Location.CharacterIndex,
                error.Location.LineIndex + 1,
                error.Location.CharacterIndex + error.Length);
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
