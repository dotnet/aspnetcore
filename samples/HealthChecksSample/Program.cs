using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
                { "detailed", typeof(DetailedStatusStartup) },
                { "writer", typeof(CustomWriterStartup) },
                { "liveness", typeof(LivenessProbeStartup) },
            };
        }

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            var scenario = config["scenario"] ?? string.Empty;
            if (!_scenarios.TryGetValue(scenario, out var startupType))
            {
                startupType = typeof(BasicStartup);
            }

            return new WebHostBuilder()
                .UseConfiguration(config)
                .ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                })
                .UseKestrel()
                .UseStartup(startupType)
                .Build();
        }

    }
}
