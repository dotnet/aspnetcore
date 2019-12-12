// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Crankier.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            #if DEBUG
            if (args.Any(a => string.Equals(a, "--debug", StringComparison.Ordinal)))
            {
                args = args.Where(a => !string.Equals(a, "--debug", StringComparison.Ordinal)).ToArray();
                Console.WriteLine($"Waiting for debugger. Process ID: {Process.GetCurrentProcess().Id}");
                Console.WriteLine("Press ENTER to continue");
                Console.ReadLine();
            }
            #endif

            var app = new CommandLineApplication();
            app.Description = "Crank's Revenge";
            app.HelpOption("-h|--help");

            LocalCommand.Register(app);
            AgentCommand.Register(app);
            WorkerCommand.Register(app);
            ServerCommand.Register(app);

            app.Command("help", cmd =>
            {
                var commandArgument = cmd.Argument("<COMMAND>", "The command to get help for.");

                cmd.OnExecute(() =>
                {
                    app.ShowHelp(commandArgument.Value);
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            app.Execute(args);
        }
    }
}
