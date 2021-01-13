// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Components.WebAssembly.DebugProxy.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.WebAssembly.DebugProxy
{
    public class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "webassembly-debugproxy"
            };
            app.HelpOption("-?|-h|--help");

            var browserHostOption = new CommandOption("-b|--browser-host", CommandOptionType.SingleValue)
            {
                Description = "Host on which the browser is listening for debug connections. Example: http://localhost:9300"
            };

            var ownerPidOption = new CommandOption("-op|--owner-pid", CommandOptionType.SingleValue)
            {
                Description = "ID of the owner process. The debug proxy will shut down if this process exits."
            };

            app.Options.Add(browserHostOption);
            app.Options.Add(ownerPidOption);

            app.OnExecute(() =>
            {
                var browserHost = browserHostOption.HasValue() ? browserHostOption.Value(): "http://127.0.0.1:9222";
                var host = DebugProxyHost.CreateDefaultBuilder(args, browserHost).Build();

                if (ownerPidOption.HasValue())
                {
                    var ownerProcess = Process.GetProcessById(int.Parse(ownerPidOption.Value()));
                    ownerProcess.EnableRaisingEvents = true;
                    ownerProcess.Exited += async (sender, eventArgs) =>
                    {
                        Console.WriteLine("Exiting because parent process has exited");
                        await host.StopAsync();
                    };
                }

                host.Run();

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
