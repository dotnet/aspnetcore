// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// A type that uses Roslyn to compile C# content.
    /// </summary>
    public class DefaultRoslynCompilationService : ICompilationService
    {
        // error CS0234: The type or namespace name 'C' does not exist in the namespace 'N' (are you missing
        // an assembly reference?)
        private const string CS0234 = nameof(CS0234);
        // error CS0246: The type or namespace name 'T' could not be found (are you missing a using directive
        // or an assembly reference?)
        private const string CS0246 = nameof(CS0246);

        private readonly CSharpCompiler _compiler;
        private readonly ILogger _logger;
        private readonly Action<RoslynCompilationContext> _compilationCallback;

        /// <summary>
        /// Initalizes a new instance of the <see cref="DefaultRoslynCompilationService"/> class.
        /// </summary>
        /// <param name="compiler">The <see cref="CSharpCompiler"/>.</param>
        /// <param name="optionsAccessor">Accessor to <see cref="RazorViewEngineOptions"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DefaultRoslynCompilationService(
            CSharpCompiler compiler,
            IOptions<RazorViewEngineOptions> optionsAccessor,
            ILoggerFactory loggerFactory)
        {
            _compiler = compiler;
            _compilationCallback = optionsAccessor.Value.CompilationCallback;
            _logger = loggerFactory.CreateLogger<DefaultRoslynCompilationService>();
        }

        /// <inheritdoc />
        public CompilationResult Compile(RazorCodeDocument codeDocument, RazorCSharpDocument cSharpDocument)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (cSharpDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            _logger.GeneratedCodeToAssemblyCompilationStart(codeDocument.Source.FileName);

            var startTimestamp = _logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;

            var assemblyName = Path.GetRandomFileName();
            var compilation = CreateCompilation(cSharpDocument.GeneratedCode, assemblyName);

            using (var assemblyStream = new MemoryStream())
            {
                using (var pdbStream = new MemoryStream())
                {
                    var result = compilation.Emit(
                        assemblyStream,
                        pdbStream,
                        options: _compiler.EmitOptions);

                    if (!result.Success)
                    {
                        return GetCompilationFailedResult(
                            codeDocument,
                            cSharpDocument.GeneratedCode,
                            assemblyName,
                            result.Diagnostics);
                    }

                    assemblyStream.Seek(0, SeekOrigin.Begin);
                    pdbStream.Seek(0, SeekOrigin.Begin);

                    var assembly = LoadAssembly(assemblyStream, pdbStream);
                    var type = assembly.GetExportedTypes().FirstOrDefault(a => !a.IsNested);

                    _logger.GeneratedCodeToAssemblyCompilationEnd(codeDocument.Source.FileName, startTimestamp);

                    return new CompilationResult(type);
                }
            }
        }

        private CSharpCompilation CreateCompilation(string compilationContent, string assemblyName)
        {
            var sourceText = SourceText.From(compilationContent, Encoding.UTF8);
            var syntaxTree = _compiler.CreateSyntaxTree(sourceText).WithFilePath(assemblyName);
            var compilation = _compiler
                .CreateCompilation(assemblyName)
                .AddSyntaxTrees(syntaxTree);
            compilation = ExpressionRewriter.Rewrite(compilation);

            var compilationContext = new RoslynCompilationContext(compilation);
            _compilationCallback(compilationContext);
            compilation = compilationContext.Compilation;
            return compilation;
        }

        // Internal for unit testing
        internal CompilationResult GetCompilationFailedResult(
            RazorCodeDocument codeDocument,
            string compilationContent,
            string assemblyName,
            IEnumerable<Diagnostic> diagnostics)
        {
            var diagnosticGroups = diagnostics
                .Where(IsError)
                .GroupBy(diagnostic => GetFilePath(codeDocument, diagnostic), StringComparer.Ordinal);

            var failures = new List<CompilationFailure>();
            foreach (var group in diagnosticGroups)
            {
                var sourceFilePath = group.Key;
                string sourceFileContent;
                if (string.Equals(assemblyName, sourceFilePath, StringComparison.Ordinal))
                {
                    // The error is in the generated code and does not have a mapping line pragma
                    sourceFileContent = compilationContent;
                    sourceFilePath = Resources.GeneratedCodeFileName;
                }
                else
                {
                    sourceFileContent = GetContent(codeDocument, sourceFilePath);
                }

                string additionalMessage = null;
                if (group.Any(g =>
                    string.Equals(CS0234, g.Id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(CS0246, g.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    additionalMessage = Resources.FormatCompilation_DependencyContextIsNotSpecified(
                        "Microsoft.NET.Sdk.Web",
                        "PreserveCompilationContext");
                }

                var compilationFailure = new CompilationFailure(
                    sourceFilePath,
                    sourceFileContent,
                    compilationContent,
                    group.Select(GetDiagnosticMessage),
                    additionalMessage);

                failures.Add(compilationFailure);
            }

            return new CompilationResult(failures);
        }

        private static string GetFilePath(RazorCodeDocument codeDocument, Diagnostic diagnostic)
        {
            if (diagnostic.Location == Location.None)
            {
                return codeDocument.Source.FileName;
            }

            return diagnostic.Location.GetMappedLineSpan().Path;
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }

        public static Assembly LoadAssembly(MemoryStream assemblyStream, MemoryStream pdbStream)
        {
            var assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream, pdbStream);
            return assembly;
        }

        private static string GetContent(RazorCodeDocument codeDocument, string filePath)
        {
            if (filePath == codeDocument.Source.FileName)
            {
                var chars = new char[codeDocument.Source.Length];
                codeDocument.Source.CopyTo(0, chars, 0, chars.Length);
                return new string(chars);
            }

            for (var i = 0; i < codeDocument.Imports.Count; i++)
            {
                var import = codeDocument.Imports[i];
                if (filePath == import.FileName)
                {
                    var chars = new char[codeDocument.Source.Length];
                    codeDocument.Source.CopyTo(0, chars, 0, chars.Length);
                    return new string(chars);
                }
            }

            return null;
        }

        private static DiagnosticMessage GetDiagnosticMessage(Diagnostic diagnostic)
        {
            var mappedLineSpan = diagnostic.Location.GetMappedLineSpan();
            return new DiagnosticMessage(
                diagnostic.GetMessage(),
                CSharpDiagnosticFormatter.Instance.Format(diagnostic),
                mappedLineSpan.Path,
                mappedLineSpan.StartLinePosition.Line + 1,
                mappedLineSpan.StartLinePosition.Character + 1,
                mappedLineSpan.EndLinePosition.Line + 1,
                mappedLineSpan.EndLinePosition.Character + 1);
        }
    }
}
