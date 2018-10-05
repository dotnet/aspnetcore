// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using System;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Blazor.Cli.Commands;

namespace Microsoft.AspNetCore.Blazor.Cli
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "blazor-cli"
            };
            app.HelpOption("-?|-h|--help");

            app.Command("serve", ServeCommand.Command);

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
