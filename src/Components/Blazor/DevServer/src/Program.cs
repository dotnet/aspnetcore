// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.DevServer.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Blazor.DevServer
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "blazor-devserver"
            };
            app.HelpOption("-?|-h|--help");

            app.Commands.Add(new ServeCommand(app));

            // A command is always required
            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            try
            {
                return app.Execute(args);
            }
            catch (CommandParsingException cex)
            {
                app.Error.WriteLine(cex.Message);
                app.ShowHelp();
                return 1;
            }
        }
    }
}
