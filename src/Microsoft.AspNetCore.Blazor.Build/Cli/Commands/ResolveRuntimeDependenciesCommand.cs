// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Blazor.Build.Cli.Commands
{
    class ResolveRuntimeDependenciesCommand
    {
        public static void Command(CommandLineApplication command)
        {
            var references = command.Option("--reference",
                "Full path to a referenced assembly file",
                CommandOptionType.MultipleValue);

            var baseClassLibrary = command.Option("--base-class-library",
                "Full path to a directory in which BCL assemblies can be found",
                CommandOptionType.MultipleValue);

            var outputPath = command.Option("--output",
                "Path to the output file that will contain the list with the full paths of the resolved assemblies",
                CommandOptionType.SingleValue);

            var mainAssemblyPath = command.Argument("assembly",
                "Path to the assembly containing the entry point of the application.");

            command.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(mainAssemblyPath.Value) ||
                    !baseClassLibrary.HasValue() || !outputPath.HasValue())
                {
                    command.ShowHelp(command.Name);
                    return 1;
                }

                try
                {
                    RuntimeDependenciesResolver.ResolveRuntimeDependencies(
                        mainAssemblyPath.Value,
                        references.Values.ToArray(),
                        baseClassLibrary.Values.ToArray(),
                        outputPath.Value());

                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return 1;
                }
            });
        }
    }
}
