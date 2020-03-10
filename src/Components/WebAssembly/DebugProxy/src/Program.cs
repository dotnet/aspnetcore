// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

            app.Options.Add(browserHostOption);

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
                    .ConfigureServices(serviceCollection =>
                    {
                        serviceCollection.AddSingleton(new DebugProxyOptions
                        {
                            BrowserHost = browserHostOption.HasValue()
                                ? browserHostOption.Value()
                                : "http://127.0.0.1:9300",
                        });
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
