// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.NodeServices.Npm;
using Microsoft.AspNetCore.NodeServices.Util;
using Microsoft.AspNetCore.SpaServices.Extensions.Util;
using Microsoft.AspNetCore.SpaServices.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    internal static class AngularCliMiddleware
    {
        private const string LogCategoryName = "Microsoft.AspNetCore.SpaServices";
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine

        public static void Attach(
            ISpaBuilder spaBuilder,
            string scriptName)
        {
            var pkgManagerCommand = spaBuilder.Options.PackageManagerCommand;
            var sourcePath = spaBuilder.Options.SourcePath;
            var devServerPort = spaBuilder.Options.DevServerPort;
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("Property 'SourcePath' cannot be null or empty", nameof(spaBuilder));
            }

            if (string.IsNullOrEmpty(scriptName))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(scriptName));
            }

            // Start Angular CLI and attach to middleware pipeline
            var appBuilder = spaBuilder.ApplicationBuilder;
            var applicationStoppingToken = appBuilder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);
            var diagnosticSource = appBuilder.ApplicationServices.GetRequiredService<DiagnosticSource>();
            var angularCliServerInfoTask = StartAngularCliServerAsync(sourcePath, scriptName, pkgManagerCommand, devServerPort, logger, diagnosticSource, applicationStoppingToken);

            SpaProxyingExtensions.UseProxyToSpaDevelopmentServer(spaBuilder, () =>
            {
                // On each request, we create a separate startup task with its own timeout. That way, even if
                // the first request times out, subsequent requests could still work.
                var timeout = spaBuilder.Options.StartupTimeout;
                return angularCliServerInfoTask.WithTimeout(timeout,
                    $"The Angular CLI process did not start listening for requests " +
                    $"within the timeout period of {timeout.TotalSeconds} seconds. " +
                    $"Check the log output for error information.");
            });
        }

        private static async Task<Uri> StartAngularCliServerAsync(
            string sourcePath, string scriptName, string pkgManagerCommand, int portNumber, ILogger logger, DiagnosticSource diagnosticSource, CancellationToken applicationStoppingToken)
        {
            if (portNumber == default(int))
            {
                portNumber = TcpPortFinder.FindAvailablePort();
            }
            logger.LogInformation($"Starting @angular/cli on port {portNumber}...");

            var scriptRunner = new NodeScriptRunner(
                sourcePath, scriptName, $"--port {portNumber}", null, pkgManagerCommand, diagnosticSource, applicationStoppingToken);
            scriptRunner.AttachToLogger(logger);

            Match openBrowserLine;
            using (var stdErrReader = new EventedStreamStringReader(scriptRunner.StdErr))
            {
                try
                {
                    openBrowserLine = await scriptRunner.StdOut.WaitForMatch(
                        new Regex("open your browser on (http\\S+)", RegexOptions.None, RegexMatchTimeout));
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The {pkgManagerCommand} script '{scriptName}' exited without indicating that the " +
                        $"Angular CLI was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
            }

            var uri = new Uri(openBrowserLine.Groups[1].Value);

            // Even after the Angular CLI claims to be listening for requests, there's a short
            // period where it will give an error if you make a request too quickly
            await WaitForAngularCliServerToAcceptRequests(uri);

            return uri;
        }

        private static async Task WaitForAngularCliServerToAcceptRequests(Uri cliServerUri)
        {
            // To determine when it's actually ready, try making HEAD requests to '/'. If it
            // produces any HTTP response (even if it's 404) then it's ready. If it rejects the
            // connection then it's not ready. We keep trying forever because this is dev-mode
            // only, and only a single startup attempt will be made, and there's a further level
            // of timeouts enforced on a per-request basis.
            var timeoutMilliseconds = 1000;
            using (var client = new HttpClient())
            {
                while (true)
                {
                    try
                    {
                        // If we get any HTTP response, the CLI server is ready
                        await client.SendAsync(
                            new HttpRequestMessage(HttpMethod.Head, cliServerUri),
                            new CancellationTokenSource(timeoutMilliseconds).Token);
                        return;
                    }
                    catch (Exception)
                    {
                        await Task.Delay(500);

                        // Depending on the host's networking configuration, the requests can take a while
                        // to go through, most likely due to the time spent resolving 'localhost'.
                        // Each time we have a failure, allow a bit longer next time (up to a maximum).
                        // This only influences the time until we regard the dev server as 'ready', so it
                        // doesn't affect the runtime perf (even in dev mode) once the first connection is made.
                        // Resolves https://github.com/aspnet/JavaScriptServices/issues/1611
                        if (timeoutMilliseconds < 10000)
                        {
                            timeoutMilliseconds += 3000;
                        }
                    }
                }
            }
        }
    }
}
