// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.BuildTools.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Blazor.BuildTools
{
    static class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "blazor-buildtools"
            };
            app.HelpOption("-?|-h|--help");

            app.Command("checknodejs", CheckNodeJsInstalled.Command);

            if (args.Length > 0)
            {
                return app.Execute(args);
            }
            else
            {
                app.ShowHelp();
                return 0;
            }
        }
    }
}
