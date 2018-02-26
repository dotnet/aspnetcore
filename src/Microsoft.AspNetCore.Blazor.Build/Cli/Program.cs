// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Build.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Blazor.Build
{
    static class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "dotnet-blazor"
            };
            app.HelpOption("-?|-h|--help");
            
            app.Command("build", BuildCommand.Command);
            app.Command("buildrazor", BuildRazorCommand.Command);
            app.Command("serve", command => ServeDevHost.Command(command, args));

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
