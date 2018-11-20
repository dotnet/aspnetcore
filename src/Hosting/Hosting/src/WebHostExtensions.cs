// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Internal;

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
        /// Block the calling thread until shutdown is triggered via Ctrl+C or SIGTERM.
        /// </summary>
        /// <param name="host">The running <see cref="IWebHost"/>.</param>
        public static void WaitForShutdown(this IWebHost host)
        {
            host.WaitForShutdownAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns a Task that completes when shutdown is triggered via the given token, Ctrl+C or SIGTERM.
        /// </summary>
        /// <param name="host">The running <see cref="IWebHost"/>.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        public static async Task WaitForShutdownAsync(this IWebHost host, CancellationToken token = default)
        {
            var done = new ManualResetEventSlim(false);
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                AttachCtrlcSigtermShutdown(cts, done, shutdownMessage: string.Empty);

                try
                {
                    await host.WaitForTokenShutdownAsync(cts.Token);
                }
                finally
                {
                    done.Set();
                }
            }
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
        /// Runs a web application and returns a Task that only completes when the token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="host">The <see cref="IWebHost"/> to run.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        public static async Task RunAsync(this IWebHost host, CancellationToken token = default)
        {
            // Wait for token shutdown if it can be canceled
            if (token.CanBeCanceled)
            {
                await host.RunAsync(token, shutdownMessage: null);
                return;
            }

            // If token cannot be canceled, attach Ctrl+C and SIGTERM shutdown
            var done = new ManualResetEventSlim(false);
            using (var cts = new CancellationTokenSource())
            {
                var shutdownMessage = host.Services.GetRequiredService<WebHostOptions>().SuppressStatusMessages ? string.Empty : "Application is shutting down...";
                AttachCtrlcSigtermShutdown(cts, done, shutdownMessage: shutdownMessage);

                try
                {
                    await host.RunAsync(cts.Token, "Application started. Press Ctrl+C to shut down.");
                }
                finally
                {
                    done.Set();
                }
            }
        }

        private static async Task RunAsync(this IWebHost host, CancellationToken token, string shutdownMessage)
        {
            using (host)
            {
                await host.StartAsync(token);

                var hostingEnvironment = host.Services.GetService<IHostingEnvironment>();
                var options = host.Services.GetRequiredService<WebHostOptions>();

                if (!options.SuppressStatusMessages)
                {
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
                }

                await host.WaitForTokenShutdownAsync(token);
            }
        }

        private static void AttachCtrlcSigtermShutdown(CancellationTokenSource cts, ManualResetEventSlim resetEvent, string shutdownMessage)
        {
            void Shutdown()
            {
                if (!cts.IsCancellationRequested)
                {
                    if (!string.IsNullOrEmpty(shutdownMessage))
                    {
                        Console.WriteLine(shutdownMessage);
                    }
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException) { }
                }

                // Wait on the given reset event
                resetEvent.Wait();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Shutdown();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Shutdown();
                // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                eventArgs.Cancel = true;
            };
        }

        private static async Task WaitForTokenShutdownAsync(this IWebHost host, CancellationToken token)
        {
            var applicationLifetime = host.Services.GetService<IApplicationLifetime>();

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