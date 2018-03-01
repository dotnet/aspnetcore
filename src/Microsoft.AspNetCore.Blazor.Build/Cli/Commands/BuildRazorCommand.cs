// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Blazor.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Blazor.Build.Cli.Commands
{
    internal class BuildRazorCommand
    {
        public static void Command(CommandLineApplication command)
        {
            // Later, we might want to have the complete list of inputs passed in from MSBuild
            // so developers can include/exclude whatever they want. The MVC Razor view precompiler
            // does this by writing the list to a temporary 'response' file then passing the path
            // to that file into its build executable (see: https://github.com/aspnet/MvcPrecompilation/blob/dev/src/Microsoft.AspNetCore.Mvc.Razor.ViewCompilation/build/netstandard2.0/Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.targets)
            // For now it's sufficient to assume we want to include '<sourcedir>**\*.cshtml'
            var sourceDirPath = command.Option("--source",
                "The path to the directory containing Razor files",
                CommandOptionType.SingleValue);
            var outputFilePath = command.Option("--output",
                "The location where the resulting C# source file should be written",
                CommandOptionType.SingleValue);
            var baseNamespace = command.Option("--namespace",
                "The base namespace for the generated C# classes.",
                CommandOptionType.SingleValue);
            var verboseFlag = command.Option("--verbose",
                "Indicates that verbose console output should written",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                if (!VerifyRequiredOptionsProvided(sourceDirPath, outputFilePath, baseNamespace))
                {
                    return 1;
                }

                var sourceDirPathValue = sourceDirPath.Value();
                if (!Directory.Exists(sourceDirPathValue))
                {
                    Console.WriteLine($"ERROR: Directory not found: {sourceDirPathValue}");
                    return 1;
                }

                var fileSystem = RazorProjectFileSystem.Create(sourceDirPathValue);
                var engine = RazorProjectEngine.Create(BlazorExtensionInitializer.DefaultConfiguration, fileSystem, b =>
                {
                    BlazorExtensionInitializer.Register(b);
                });

                var diagnostics = new List<RazorDiagnostic>();
                var sourceFiles = FindRazorFiles(sourceDirPathValue).ToList();
                using (var outputWriter = new StreamWriter(outputFilePath.Value()))
                {
                    foreach (var sourceFile in sourceFiles)
                    {
                        var item = fileSystem.GetItem(sourceFile);

                        var codeDocument = engine.Process(item);
                        var cSharpDocument = codeDocument.GetCSharpDocument();

                        outputWriter.WriteLine(cSharpDocument.GeneratedCode);
                        diagnostics.AddRange(cSharpDocument.Diagnostics);
                    }
                }

                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine(diagnostic.ToString());
                }

                var hasError = diagnostics.Any(item => item.Severity == RazorDiagnosticSeverity.Error);
                return hasError ? 1 : 0;
            });
        }

        private static IEnumerable<string> FindRazorFiles(string rootDirPath)
            => Directory.GetFiles(rootDirPath, "*.cshtml", SearchOption.AllDirectories);

        private static bool VerifyRequiredOptionsProvided(params CommandOption[] options)
        {
            var violations = options.Where(o => !o.HasValue()).ToList();
            foreach (var violation in violations)
            {
                Console.WriteLine($"ERROR: No value specified for required option '{violation.LongName}'.");
            }

            return !violations.Any();
        }
    }
}
