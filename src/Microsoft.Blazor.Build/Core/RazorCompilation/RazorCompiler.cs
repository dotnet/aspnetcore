// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Blazor.Build.Core.RazorCompilation
{
    /// <summary>
    /// Provides facilities for transforming Razor files into Blazor component classes.
    /// </summary>
    public class RazorCompiler
    {
        // TODO: Relax this to allow for whatever C# allows
        private static Regex ClassNameRegex
            = new Regex("^[a-z][a-z0-9_]*$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Writes C# source code representing Blazor components defined by Razor files.
        /// </summary>
        /// <param name="inputPaths">Paths to the input files.</param>
        /// <param name="outputNamespace">The namespace for the generated classes.</param>
        /// <param name="resultOutput">A <see cref="TextWriter"/> to which C# source code will be written.</param>
        /// <param name="verboseOutput">If not null, additional information will be written to this <see cref="TextWriter"/>.</param>
        /// <returns>A collection of <see cref="RazorCompilerDiagnostic"/> instances representing any warnings or errors that were encountered.</returns>
        public ICollection<RazorCompilerDiagnostic> CompileFiles(IEnumerable<string> inputPaths, string outputNamespace, TextWriter resultOutput, TextWriter verboseOutput)
            => inputPaths.SelectMany(path =>
            {
                using (var reader = File.OpenText(path))
                {
                    return CompileSingleFile(path, reader, outputNamespace, resultOutput, verboseOutput);
                }
            }).ToList();

        /// <summary>
        /// Writes C# source code representing a Blazor component defined by a Razor file.
        /// </summary>
        /// <param name="inputPaths">The path to the input file.</param>
        /// <param name="outputNamespace">The namespace for the generated class.</param>
        /// <param name="resultOutput">A <see cref="TextWriter"/> to which C# source code will be written.</param>
        /// <param name="verboseOutput">If not null, additional information will be written to this <see cref="TextWriter"/>.</param>
        /// <returns>An enumerable of <see cref="RazorCompilerDiagnostic"/> instances representing any warnings or errors that were encountered.</returns>
        public IEnumerable<RazorCompilerDiagnostic> CompileSingleFile(string inputFilePath, TextReader inputFileReader, string outputNamespace, TextWriter resultOutput, TextWriter verboseOutput)
        {
            if (resultOutput == null)
            {
                throw new ArgumentNullException(nameof(resultOutput));
            }

            try
            {
                verboseOutput?.WriteLine($"Compiling {inputFilePath}...");
                var className = GetClassName(inputFilePath);

                resultOutput.WriteLine($"namespace {outputNamespace} {{");
                resultOutput.WriteLine($"public class {className}");
                resultOutput.WriteLine("{");
                resultOutput.WriteLine("}");
                resultOutput.WriteLine("}");
                resultOutput.WriteLine();

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

        private static string GetClassName(string inputFilePath)
        {
            var basename = Path.GetFileNameWithoutExtension(inputFilePath);
            if (!ClassNameRegex.IsMatch(basename))
            {
                throw new RazorCompilerException($"Invalid name '{basename}'. The name must be valid for a C# class name.");
            }

            return basename;
        }
    }
}
