// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
