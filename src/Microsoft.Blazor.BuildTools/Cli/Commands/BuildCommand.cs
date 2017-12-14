// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.BuildTools.Core;
using Microsoft.Extensions.CommandLineUtils;
using System;

namespace Microsoft.Blazor.BuildTools.Cli.Commands
{
    internal class BuildCommand
    {
        public static void Command(CommandLineApplication command)
        {
            var clientAssemblyPath = command.Argument("assembly",
                "Specifies the assembly for the Blazor application.");
            var webRootPath = command.Option("--webroot",
                "Specifies the path to the directory containing static files to be served",
                CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(clientAssemblyPath.Value))
                {
                    Console.WriteLine($"ERROR: No value specified for required argument '{clientAssemblyPath.Name}'.");
                    return 1;
                }

                try
                {
                    Console.WriteLine($"Building Blazor app from {clientAssemblyPath.Value}...");
                    Build.Execute(clientAssemblyPath.Value, webRootPath.HasValue() ? webRootPath.Value() : null);
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
