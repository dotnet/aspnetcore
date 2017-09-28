// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Internal
{
    /// <summary>
    /// Listens for Ctrl+C or SIGTERM and initiates shutdown.
    /// </summary>
    public class ConsoleLifetime : IHostLifetime
    {
        public ConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostingEnvironment environment, IApplicationLifetime applicationLifetime)
        {
            Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        }

        private ConsoleLifetimeOptions Options { get; }

        private IHostingEnvironment Environment { get; }

        private IApplicationLifetime ApplicationLifetime { get; }

        public void RegisterDelayStartCallback(Action<object> callback, object state)
        {
            if (!Options.SuppressStatusMessages)
            {
                ApplicationLifetime.ApplicationStarted.Register(() =>
                {
                    Console.WriteLine("Application started. Press Ctrl+C to shut down.");
                    Console.WriteLine($"Hosting environment: {Environment.EnvironmentName}");
                    Console.WriteLine($"Content root path: {Environment.ContentRootPath}");
                });
            }

            // Console applications start immediately.
            callback(state);
        }

        public void RegisterStopCallback(Action<object> callback, object state)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => callback(state);
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                callback(state);
            };
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // There's nothing to do here
            return Task.CompletedTask;
        }
    }
}
