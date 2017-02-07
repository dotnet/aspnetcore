// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IRazorCompilationService"/>.
    /// </summary>
    public class RazorCompilationService : IRazorCompilationService
    {
        private readonly ICompilationService _compilationService;
        private readonly RazorEngine _engine;
        private readonly RazorProject _project;
        private readonly IFileProvider _fileProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new instance of the <see cref="RazorCompilationService"/> class.
        /// </summary>
        /// <param name="compilationService">The <see cref="ICompilationService"/> to compile generated code.</param>
        /// <param name="engine">The <see cref="RazorEngine"/> to generate code from Razor files.</param>
        /// <param name="project">The <see cref="RazorProject"/> implementation for locating files.</param>
        /// <param name="fileProviderAccessor">The <see cref="IRazorViewEngineFileProviderAccessor"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public RazorCompilationService(
            ICompilationService compilationService,
            RazorEngine engine,
            RazorProject project,
            IRazorViewEngineFileProviderAccessor fileProviderAccessor,
            ILoggerFactory loggerFactory)
        {
            _compilationService = compilationService;
            _engine = engine;
            _fileProvider = fileProviderAccessor.FileProvider;
            _logger = loggerFactory.CreateLogger<RazorCompilationService>();

            _project = project;

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.WriteLine("@using System");
            writer.WriteLine("@using System.Linq");
            writer.WriteLine("@using System.Collections.Generic");
            writer.WriteLine("@using Microsoft.AspNetCore.Mvc");
            writer.WriteLine("@using Microsoft.AspNetCore.Mvc.Rendering");
            writer.WriteLine("@using Microsoft.AspNetCore.Mvc.ViewFeatures");
            writer.WriteLine("@inject Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> Html");
            writer.WriteLine("@inject Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json");
            writer.WriteLine("@inject Microsoft.AspNetCore.Mvc.IViewComponentHelper Component");
            writer.WriteLine("@inject Microsoft.AspNetCore.Mvc.IUrlHelper Url");
            writer.WriteLine("@inject Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider");
            writer.WriteLine("@addTagHelper Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper, Microsoft.AspNetCore.Mvc.Razor");
            writer.Flush();

            stream.Seek(0L, SeekOrigin.Begin);
            GlobalImports = RazorSourceDocument.ReadFrom(stream, filename: null, encoding: Encoding.UTF8);
        }

        public RazorSourceDocument GlobalImports { get; }

        /// <inheritdoc />
        public CompilationResult Compile(RelativeFileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            RazorCodeDocument codeDocument;
            RazorCSharpDocument cSharpDocument;
            using (var inputStream = file.FileInfo.CreateReadStream())
            {
                _logger.RazorFileToCodeCompilationStart(file.RelativePath);

                var startTimestamp = _logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;

                codeDocument = CreateCodeDocument(file.RelativePath, inputStream);
                cSharpDocument = ProcessCodeDocument(codeDocument);

                _logger.RazorFileToCodeCompilationEnd(file.RelativePath, startTimestamp);
            }

            if (cSharpDocument.Diagnostics.Count > 0)
            {
                return GetCompilationFailedResult(file.RelativePath, cSharpDocument.Diagnostics);
            }

            return _compilationService.Compile(codeDocument, cSharpDocument);
        }

        public virtual RazorCodeDocument CreateCodeDocument(string relativePath, Stream inputStream)
        {
            var absolutePath = _fileProvider.GetFileInfo(relativePath)?.PhysicalPath ?? relativePath;

            var source = RazorSourceDocument.ReadFrom(inputStream, absolutePath);

            var imports = new List<RazorSourceDocument>()
            {
                GlobalImports,
            };

            var paths = ViewHierarchyUtility.GetViewImportsLocations(relativePath);
            foreach (var path in paths.Reverse())
            {
                var file = _fileProvider.GetFileInfo(path);
                if (file.Exists)
                {
                    using (var stream = file.CreateReadStream())
                    {
                        imports.Add(RazorSourceDocument.ReadFrom(stream, file.PhysicalPath ?? path));
                    }
                }
            }

            return RazorCodeDocument.Create(source, imports);
        }

        public virtual RazorCSharpDocument ProcessCodeDocument(RazorCodeDocument codeDocument)
        {
            _engine.Process(codeDocument);

            return codeDocument.GetCSharpDocument();
        }

        // Internal for unit testing
        public CompilationResult GetCompilationFailedResult(
            string relativePath,
            IEnumerable<Microsoft.AspNetCore.Razor.Evolution.Legacy.RazorError> errors)
        {
            // If a SourceLocation does not specify a file path, assume it is produced
            // from parsing the current file.
            var messageGroups = errors
                .GroupBy(razorError =>
                razorError.Location.FilePath ?? relativePath,
                StringComparer.Ordinal);

            var failures = new List<CompilationFailure>();
            foreach (var group in messageGroups)
            {
                var filePath = group.Key;
                var fileContent = ReadFileContentsSafely(filePath);
                var compilationFailure = new CompilationFailure(
                    filePath,
                    fileContent,
                    compiledContent: string.Empty,
                    messages: group.Select(parserError => CreateDiagnosticMessage(parserError, filePath)));
                failures.Add(compilationFailure);
            }

            return new CompilationResult(failures);
        }

        private DiagnosticMessage CreateDiagnosticMessage(
            Microsoft.AspNetCore.Razor.Evolution.Legacy.RazorError error,
            string filePath)
        {
            var location = error.Location;
            return new DiagnosticMessage(
                message: error.Message,
                formattedMessage: $"{error} ({location.LineIndex},{location.CharacterIndex}) {error.Message}",
                filePath: filePath,
                startLine: error.Location.LineIndex + 1,
                startColumn: error.Location.CharacterIndex,
                endLine: error.Location.LineIndex + 1,
                endColumn: error.Location.CharacterIndex + error.Length);
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
