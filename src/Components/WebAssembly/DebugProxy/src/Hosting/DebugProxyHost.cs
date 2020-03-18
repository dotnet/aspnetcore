using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.DebugProxy.Hosting
{
    public static class DebugProxyHost
    {
        /// <summary>
        /// Creates a custom HostBuilder for the DebugProxyLauncher so that we can inject
        /// only the needed configurations.
        /// </summary>
        /// <param name="args">Command line arguments passed in</param>
        /// <param name="browserHost">Host where browser is listening for debug connections</param>
        /// <returns><see cref="IHostBuilder"></returns>
        public static IHostBuilder CreateDefaultBuilder(string[] args, string browserHost)
        {
            var builder = new HostBuilder();

            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
                config.AddJsonFile("blazor-debugproxysettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();

                // By default we bind to a dyamic port
                // This can be overridden using an option like "--urls http://localhost:9500"
                webBuilder.UseUrls("http://127.0.0.1:0");
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            })
            .ConfigureServices(serviceCollection =>
            {
                serviceCollection.AddSingleton(new DebugProxyOptions
                {
                    BrowserHost = browserHost
                });
            });

            return builder;

        }
    }
}
