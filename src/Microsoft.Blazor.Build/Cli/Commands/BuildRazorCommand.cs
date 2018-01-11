// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Build.Core.RazorCompilation;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Blazor.Build.Cli.Commands
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
            var verboseFlag = command.Option("--verbose",
                "Indicates that verbose console output should written",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                if (!VerifyRequiredOptionsProvided(sourceDirPath, outputFilePath))
                {
                    return 1;
                }

                var sourceDirPathValue = sourceDirPath.Value();
                if (!Directory.Exists(sourceDirPathValue))
                {
                    Console.WriteLine($"ERROR: Directory not found: {sourceDirPathValue}");
                    return 1;
                }

                var inputRazorFilePaths = FindRazorFiles(sourceDirPathValue).ToList();
                using (var outputWriter = new StreamWriter(outputFilePath.Value()))
                {
                    var diagnostics = new RazorCompiler().CompileFiles(
                        inputRazorFilePaths,
                        "Blazor", // TODO: Add required option for namespace
                        outputWriter,
                        verboseFlag.HasValue() ? Console.Out : null);

                    foreach (var diagnostic in diagnostics)
                    {
                        Console.WriteLine(diagnostic.FormatForConsole());
                    }

                    var hasError = diagnostics.Any(item => item.Type == RazorCompilerDiagnostic.DiagnosticType.Error);
                    return hasError ? 1 : 0;
                }
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
