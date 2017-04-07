
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class RazorCompiler
    {
        private readonly ICompilationService _compilationService;
        private readonly ICompilerCacheProvider _compilerCacheProvider;
        private readonly RazorTemplateEngine _templateEngine;
        private readonly Func<string, CompilerCacheContext> _getCacheContext;
        private readonly Func<CompilerCacheContext, CompilationResult> _getCompilationResultDelegate;

        public RazorCompiler(
            ICompilationService compilationService,
            ICompilerCacheProvider compilerCacheProvider,
            RazorTemplateEngine templateEngine)
        {
            _compilationService = compilationService;
            _compilerCacheProvider = compilerCacheProvider;
            _templateEngine = templateEngine;
            _getCacheContext = GetCacheContext;
            _getCompilationResultDelegate = GetCompilationResult;
        }

        private ICompilerCache CompilerCache => _compilerCacheProvider.Cache;

        public CompilerCacheResult Compile(string relativePath)
        {
            return CompilerCache.GetOrAdd(relativePath, _getCacheContext);
        }

        private CompilerCacheContext GetCacheContext(string path)
        {
            var item = _templateEngine.Project.GetItem(path);
            var imports = _templateEngine.Project.FindHierarchicalItems(path, _templateEngine.Options.ImportsFileName);
            return new CompilerCacheContext(item, imports, GetCompilationResult);
        }

        private CompilationResult GetCompilationResult(CompilerCacheContext cacheContext)
        {
            var projectItem = cacheContext.ProjectItem;
            var codeDocument = _templateEngine.CreateCodeDocument(projectItem.Path);
            var cSharpDocument = _templateEngine.GenerateCode(codeDocument);

            CompilationResult compilationResult;
            if (cSharpDocument.Diagnostics.Count > 0)
            {
                compilationResult = GetCompilationFailedResult(
                    codeDocument,
                    cSharpDocument.Diagnostics);
            }
            else
            {
                compilationResult = _compilationService.Compile(codeDocument, cSharpDocument);
            }

            return compilationResult;
        }

        internal CompilationResult GetCompilationFailedResult(
            RazorCodeDocument codeDocument,
            IEnumerable<RazorDiagnostic> diagnostics)
        {
            // If a SourceLocation does not specify a file path, assume it is produced from parsing the current file.
            var messageGroups = diagnostics.GroupBy(
                razorError => razorError.Span.FilePath ?? codeDocument.Source.FileName,
                StringComparer.Ordinal);

            var failures = new List<CompilationFailure>();
            foreach (var group in messageGroups)
            {
                var filePath = group.Key;
                var fileContent = ReadContent(codeDocument, filePath);
                var compilationFailure = new CompilationFailure(
                    filePath,
                    fileContent,
                    compiledContent: string.Empty,
                    messages: group.Select(parserError => CreateDiagnosticMessage(parserError, filePath)));
                failures.Add(compilationFailure);
            }

            return new CompilationResult(failures);
        }

        private static string ReadContent(RazorCodeDocument codeDocument, string filePath)
        {
            RazorSourceDocument sourceDocument = null;
            if (string.IsNullOrEmpty(filePath) || string.Equals(codeDocument.Source.FileName, filePath, StringComparison.Ordinal))
            {
                sourceDocument = codeDocument.Source;
            }
            else
            {
                sourceDocument = codeDocument.Imports.FirstOrDefault(f => string.Equals(f.FileName, filePath, StringComparison.Ordinal));
            }

            if (sourceDocument != null)
            {
                var contentChars = new char[sourceDocument.Length];
                sourceDocument.CopyTo(0, contentChars, 0, sourceDocument.Length);
                return new string(contentChars);
            }

            return string.Empty;
        }

        private static DiagnosticMessage CreateDiagnosticMessage(
            RazorDiagnostic razorDiagnostic,
            string filePath)
        {
            var sourceSpan = razorDiagnostic.Span;
            var message = razorDiagnostic.GetMessage();
            return new DiagnosticMessage(
                message: message,
                formattedMessage: razorDiagnostic.ToString(),
                filePath: filePath,
                startLine: sourceSpan.LineIndex + 1,
                startColumn: sourceSpan.CharacterIndex,
                endLine: sourceSpan.LineIndex + 1,
                endColumn: sourceSpan.CharacterIndex + sourceSpan.Length);
        }
    }
}
