using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HealthChecksSample
{
    public class Program
    {
        private static readonly Dictionary<string, Type> _scenarios;

        static Program()
        {
            _scenarios = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "", typeof(BasicStartup) },
                { "basic", typeof(BasicStartup) },
                { "writer", typeof(CustomWriterStartup) },
                { "liveness", typeof(LivenessProbeStartup) },
                { "port", typeof(ManagementPortStartup) },
                { "db", typeof(DbHealthStartup) },
                { "dbcontext", typeof(DbContextHealthStartup) },
            };
        }

        public static Task Main(string[] args)
        {
            return BuildWebHost(args).RunAsync();
        }

        public static IHost BuildWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            var scenario = config["scenario"] ?? string.Empty;
            if (!_scenarios.TryGetValue(scenario, out var startupType))
            {
                startupType = typeof(BasicStartup);
            }

            return new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseConfiguration(config)
                    .ConfigureLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Trace);
                        builder.AddConfiguration(config);
                        builder.AddConsole();
                    })
                    .UseKestrel()
                    .UseStartup(startupType);
                })
                .Build();
        }

    }
}
