// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
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

            var browserPortOption = new CommandOption("-b|--browser-port", CommandOptionType.SingleValue)
            {
                Description = "Port to which the debug proxy should connect"
            };

            app.Options.Add(browserPortOption);

            app.OnExecute(() =>
            {
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddCommandLine(args);
                    })
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();

                        // By default we bind to a dyamic port
                        // This can be overridden using an option like "--urls http://localhost:9500"
                        webBuilder.UseUrls($"http://127.0.0.1:0");
                    })
                    .Build();

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
