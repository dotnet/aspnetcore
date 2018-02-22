// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.CodeDom.Compiler;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Razor;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Build.Core.RazorCompilation
{
    /// <summary>
    /// Provides facilities for transforming Razor files into Blazor component classes.
    /// </summary>
    public class RazorCompiler
    {
        private static CodeDomProvider _csharpCodeDomProvider = CodeDomProvider.CreateProvider("c#");

        /// <summary>
        /// Writes C# source code representing Blazor components defined by Razor files.
        /// </summary>
        /// <param name="inputRootPath">Path to a directory containing input files.</param>
        /// <param name="inputPaths">Paths to the input files relative to <paramref name="inputRootPath"/>. The generated namespaces will be based on these relative paths.</param>
        /// <param name="baseNamespace">The base namespace for the generated classes.</param>
        /// <param name="resultOutput">A <see cref="TextWriter"/> to which C# source code will be written.</param>
        /// <param name="verboseOutput">If not null, additional information will be written to this <see cref="TextWriter"/>.</param>
        /// <returns>A collection of <see cref="RazorCompilerDiagnostic"/> instances representing any warnings or errors that were encountered.</returns>
        public ICollection<RazorCompilerDiagnostic> CompileFiles(
            string inputRootPath,
            IEnumerable<string> inputPaths,
            string baseNamespace,
            TextWriter resultOutput,
            TextWriter verboseOutput)
            => inputPaths.SelectMany(path =>
            {
                using (var reader = File.OpenRead(path))
                {
                    return CompileSingleFile(inputRootPath, path, reader, baseNamespace, resultOutput, verboseOutput);
                }
            }).ToList();

        /// <summary>
        /// Writes C# source code representing a Blazor component defined by a Razor file.
        /// </summary>
        /// <param name="inputRootPath">Path to a directory containing input files.</param>
        /// <param name="inputPaths">Paths to the input files relative to <paramref name="inputRootPath"/>. The generated namespaces will be based on these relative paths.</param>
        /// <param name="baseNamespace">The base namespace for the generated class.</param>
        /// <param name="resultOutput">A <see cref="TextWriter"/> to which C# source code will be written.</param>
        /// <param name="verboseOutput">If not null, additional information will be written to this <see cref="TextWriter"/>.</param>
        /// <returns>An enumerable of <see cref="RazorCompilerDiagnostic"/> instances representing any warnings or errors that were encountered.</returns>
        public IEnumerable<RazorCompilerDiagnostic> CompileSingleFile(
            string inputRootPath,
            string inputFilePath,
            Stream inputFileContents,
            string baseNamespace,
            TextWriter resultOutput,
            TextWriter verboseOutput)
        {
            if (inputFileContents == null)
            {
                throw new ArgumentNullException(nameof(inputFileContents));
            }

            if (resultOutput == null)
            {
                throw new ArgumentNullException(nameof(resultOutput));
            }

            if (string.IsNullOrEmpty(inputRootPath))
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(inputRootPath));
            }

            if (string.IsNullOrEmpty(baseNamespace))
            {
                throw new ArgumentException("Cannot be null or empty.", nameof(baseNamespace));
            }

            try
            {
                verboseOutput?.WriteLine($"Compiling {inputFilePath}...");
                var (itemNamespace, itemClassName) = GetNamespaceAndClassName(inputRootPath, inputFilePath);
                var combinedNamespace = string.IsNullOrEmpty(itemNamespace)
                    ? baseNamespace
                    : $"{baseNamespace}.{itemNamespace}";

                // TODO: Pass through info about whether this is a design-time build, and if so,
                // just emit enough of a stub class that intellisense will show the correct type
                // name and any public members. Don't need to actually emit all the RenderTreeBuilder
                // invocations.

                var engine = new BlazorRazorEngine();
                var blazorTemplateEngine = new BlazorTemplateEngine(
                    engine.Engine,
                    RazorProjectFileSystem.Create(inputRootPath));
                var codeDoc = blazorTemplateEngine.CreateCodeDocument(
                    new BlazorProjectItem(inputRootPath, inputFilePath, inputFileContents));
                codeDoc.Items[BlazorCodeDocItems.Namespace] = combinedNamespace;
                codeDoc.Items[BlazorCodeDocItems.ClassName] = itemClassName;
                var csharpDocument = blazorTemplateEngine.GenerateCode(codeDoc);
                var generatedCode = csharpDocument.GeneratedCode;

                // Add parameters to the primary method via string manipulation because
                // DefaultDocumentWriter's VisitMethodDeclaration can't emit parameters
                var primaryMethodSource = $"protected override void {BlazorComponent.BuildRenderTreeMethodName}";
                generatedCode = generatedCode.Replace(
                    $"{primaryMethodSource}()",
                    $"{primaryMethodSource}({typeof(RenderTreeBuilder).FullName} builder)");

                resultOutput.WriteLine(generatedCode);

                return Enumerable.Empty<RazorCompilerDiagnostic>();
            }
            catch (RazorCompilerException ex)
            {
                return new[] { ex.ToDiagnostic(inputFilePath) };
            }
            catch (Exception ex)
            {
                return new[]
                {
                    new RazorCompilerDiagnostic(
                        RazorCompilerDiagnostic.DiagnosticType.Error,
                        inputFilePath,
                        1,
                        1,
                        $"Unexpected exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}")
                };
            }
        }

        private static (string, string) GetNamespaceAndClassName(string inputRootPath, string inputFilePath)
        {
            // First represent inputFilePath as a path relative to inputRootPath. Not using Path.GetRelativePath
            // because it doesn't handle cases like inputFilePath="\\something.cs".
            var inputFilePathAbsolute = Path.GetFullPath(Path.Combine(inputRootPath, inputFilePath));
            var inputRootPathWithTrailingSeparator = inputRootPath.EndsWith(Path.DirectorySeparatorChar)
                ? inputRootPath
                : (inputRootPath + Path.DirectorySeparatorChar);
            var inputFilePathRelative = inputFilePathAbsolute.StartsWith(inputRootPathWithTrailingSeparator)
                ? inputFilePathAbsolute.Substring(inputRootPathWithTrailingSeparator.Length)
                : throw new RazorCompilerException($"File is not within source root directory: '{inputFilePath}'");

            // Use the set of directory names in the relative path as namespace
            var inputDirname = Path.GetDirectoryName(inputFilePathRelative);
            var resultNamespace = inputDirname
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(" ", string.Empty);

            // Use the filename as class name
            var inputBasename = Path.GetFileNameWithoutExtension(inputFilePathRelative);
            if (!IsValidClassName(inputBasename))
            {
                throw new RazorCompilerException($"Invalid name '{inputBasename}'. The name must be valid for a C# class name.");
            }

            return (resultNamespace, inputBasename);
        }

        private static bool IsValidClassName(string name)
            => _csharpCodeDomProvider.IsValidIdentifier(name);
    }
}
