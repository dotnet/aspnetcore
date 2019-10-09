// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;

namespace Microsoft.AspNetCore.Blazor.Build.DevServer.Commands
{
    internal class WriteBootJsonCommand
    {
        public static void Command(CommandLineApplication command)
        {
            var referencesFile = command.Option("--references",
                "The path to a file that lists the paths to given referenced dll files",
                CommandOptionType.SingleValue);

            var embeddedResourcesFile = command.Option("--embedded-resources",
                "The path to a file that lists the paths of .NET assemblies that may contain embedded resources (typically, referenced assemblies in their pre-linked states)",
                CommandOptionType.SingleValue);

            var outputPath = command.Option("--output",
                "Path to the output file",
                CommandOptionType.SingleValue);

            var mainAssemblyPath = command.Argument("assembly",
                "Path to the assembly containing the entry point of the application.");

            var linkerEnabledFlag = command.Option("--linker-enabled",
                "If set, specifies that the application is being built with linking enabled.",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(mainAssemblyPath.Value) || !outputPath.HasValue())
                {
                    command.ShowHelp(command.Name);
                    return 1;
                }

                try
                {
                    var referencesSources = referencesFile.HasValue()
                        ? File.ReadAllLines(referencesFile.Value())
                        : Array.Empty<string>();

                    var embeddedResourcesSources = embeddedResourcesFile.HasValue()
                        ? File.ReadAllLines(embeddedResourcesFile.Value())
                        : Array.Empty<string>();

                    BootJsonWriter.WriteFile(
                        mainAssemblyPath.Value,
                        referencesSources,
                        embeddedResourcesSources,
                        linkerEnabledFlag.HasValue(),
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
