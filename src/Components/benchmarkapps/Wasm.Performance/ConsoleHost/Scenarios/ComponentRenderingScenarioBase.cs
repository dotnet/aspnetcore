// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Wasm.Performance.ConsoleHost.Scenarios
{
    internal abstract class ComponentRenderingScenarioBase : CommandLineApplication
    {
        protected ComponentRenderingScenarioBase(string name)
        {
            Name = name;

            var cyclesOption = new CommandOption("--cycles", CommandOptionType.SingleValue);
            Options.Add(cyclesOption);

            OnExecute(() =>
            {
                var numCycles = cyclesOption.HasValue() ? int.Parse(cyclesOption.Value(), CultureInfo.InvariantCulture) : 1;

                var serviceCollection = new ServiceCollection();
                PopulateServiceCollection(serviceCollection);
                var serviceProvider = serviceCollection.BuildServiceProvider();

                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var renderer = new ConsoleHostRenderer(serviceProvider, loggerFactory);

                var startTime = DateTime.Now;
                ExecuteAsync(renderer, numCycles).Wait();

                var duration = DateTime.Now - startTime;
                var durationPerCycle = (duration / numCycles).TotalMilliseconds;
                Console.WriteLine($"{Name}: {durationPerCycle:F1}ms per cycle (cycles tested: {numCycles})");

                return 0;
            });
        }

        protected virtual void PopulateServiceCollection(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging();
        }

        protected abstract Task ExecuteAsync(ConsoleHostRenderer renderer, int numCycles);
    }
}
