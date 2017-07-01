// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostExtensions
    {
        /// <summary>
        /// Attempts to gracefully stop the host with the given timeout.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="timeout">The timeout for stopping gracefully. Once expired the
        /// server may terminate any remaining active connections.</param>
        /// <returns></returns>
        public static Task StopAsync(this IWebHost host, TimeSpan timeout)
        {
            return host.StopAsync(new CancellationTokenSource(timeout).Token);
        }

        /// <summary>
        /// Runs a web application and block the calling thread until host shutdown.
        /// </summary>
        /// <param name="host">The <see cref="IWebHost"/> to run.</param>
        public static void Run(this IWebHost host)
        {
            host.RunAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs a web application and returns a Task that only completes on host shutdown.
        /// </summary>
        /// <param name="host">The <see cref="IWebHost"/> to run.</param>
        public static async Task RunAsync(this IWebHost host)
        {
            var done = new ManualResetEventSlim(false);
            using (var cts = new CancellationTokenSource())
            {
                Action shutdown = () =>
                {
                    if (!cts.IsCancellationRequested)
                    {
                        Console.WriteLine("Application is shutting down...");
                        try
                        {
                            cts.Cancel();
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }

                    done.Wait();
                };

                AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => shutdown();
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    shutdown();
                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                await host.RunAsync(cts.Token, "Application started. Press Ctrl+C to shut down.");
                done.Set();
            }
        }

        /// <summary>
        /// Runs a web application and and returns a Task that only completes when the token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="host">The <see cref="IWebHost"/> to run.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        public static Task RunAsync(this IWebHost host, CancellationToken token)
        {
            return host.RunAsync(token, shutdownMessage: null);
        }

        private static async Task RunAsync(this IWebHost host, CancellationToken token, string shutdownMessage)
        {
            using (host)
            {
                await host.StartAsync(token);

                var hostingEnvironment = host.Services.GetService<IHostingEnvironment>();
                var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

                Console.WriteLine($"Hosting environment: {hostingEnvironment.EnvironmentName}");
                Console.WriteLine($"Content root path: {hostingEnvironment.ContentRootPath}");

                var serverAddresses = host.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
                if (serverAddresses != null)
                {
                    foreach (var address in serverAddresses)
                    {
                        Console.WriteLine($"Now listening on: {address}");
                    }
                }

                if (!string.IsNullOrEmpty(shutdownMessage))
                {
                    Console.WriteLine(shutdownMessage);
                }

                token.Register(state =>
                {
                    ((IApplicationLifetime)state).StopApplication();
                },
                applicationLifetime);

                var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                applicationLifetime.ApplicationStopping.Register(obj =>
                {
                    var tcs = (TaskCompletionSource<object>)obj;
                    tcs.TrySetResult(null);
                }, waitForStop);

                await waitForStop.Task;

                // WebHost will use its default ShutdownTimeout if none is specified.
                await host.StopAsync();
            }
        }
    }
}