// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace ClientSample;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--debug"))
        {
            Console.WriteLine($"Ready for debugger to attach. Process ID: {Process.GetCurrentProcess().Id}");
            Console.Write("Press ENTER to Continue");
            Console.ReadLine();
            args = args.Except(new[] { "--debug" }).ToArray();
        }

        var app = new CommandLineApplication();
        app.FullName = "SignalR Client Samples";
        app.Description = "Client Samples for SignalR";

        RawSample.Register(app);
        HubSample.Register(app);
        StreamingSample.Register(app);
        UploadSample.Register(app);

        app.Command("help", cmd =>
        {
            cmd.Description = "Get help for the application, or a specific command";

            var commandArgument = cmd.Argument("<COMMAND>", "The command to get help for");
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
