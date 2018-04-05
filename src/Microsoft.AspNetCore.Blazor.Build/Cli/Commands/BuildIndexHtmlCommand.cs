// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Blazor.Build.Cli.Commands
{
    internal class BuildIndexHtmlCommand
    {
        public static void Command(CommandLineApplication command)
        {
            var clientPage = command.Option("--html-page",
                "Path to the HTML Page containing the Blazor bootstrap script tag.",
                CommandOptionType.SingleValue);

            var references = command.Option("--reference",
                "The path from the _bin folder to a given referenced dll file (typically just the dll name)",
                CommandOptionType.MultipleValue);

            var embeddedResourcesSources = command.Option("--embedded-resources-source",
                "The path to an assembly that may contain embedded resources (typically a referenced assembly in its pre-linked state)",
                CommandOptionType.MultipleValue);

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
                if (string.IsNullOrEmpty(mainAssemblyPath.Value) ||
                    !clientPage.HasValue() || !references.HasValue() || !outputPath.HasValue())
                {
                    command.ShowHelp(command.Name);
                    return 1;
                }

                try
                {
                    IndexHtmlWriter.UpdateIndex(
                        clientPage.Value(),
                        mainAssemblyPath.Value,
                        references.Values.ToArray(),
                        embeddedResourcesSources.Values.ToArray(),
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
