// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

namespace Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer
{
    internal static class ReactDevelopmentServerMiddleware
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

            // Start create-react-app and attach to middleware pipeline
            var appBuilder = spaBuilder.ApplicationBuilder;
            var applicationStoppingToken = appBuilder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);
            var diagnosticSource = appBuilder.ApplicationServices.GetRequiredService<DiagnosticSource>();
            var portTask = StartCreateReactAppServerAsync(sourcePath, scriptName, pkgManagerCommand, devServerPort, logger, diagnosticSource, applicationStoppingToken);

            SpaProxyingExtensions.UseProxyToSpaDevelopmentServer(spaBuilder, async () =>
            {
                // On each request, we create a separate startup task with its own timeout. That way, even if
                // the first request times out, subsequent requests could still work.
                var timeout = spaBuilder.Options.StartupTimeout;
                var port = await portTask.WithTimeout(timeout, $"The create-react-app server did not start listening for requests " +
                    $"within the timeout period of {timeout.Seconds} seconds. " +
                    $"Check the log output for error information.");

                // Everything we proxy is hardcoded to target http://localhost because:
                // - the requests are always from the local machine (we're not accepting remote
                //   requests that go directly to the create-react-app server)
                // - given that, there's no reason to use https, and we couldn't even if we
                //   wanted to, because in general the create-react-app server has no certificate
                return new UriBuilder("http", "localhost", port).Uri;
            });
        }

        private static async Task<int> StartCreateReactAppServerAsync(
            string sourcePath, string scriptName, string pkgManagerCommand, int portNumber, ILogger logger, DiagnosticSource diagnosticSource, CancellationToken applicationStoppingToken)
        {
            if (portNumber == default(int))
            {
                portNumber = TcpPortFinder.FindAvailablePort();
            }
            logger.LogInformation($"Starting create-react-app server on port {portNumber}...");

            var envVars = new Dictionary<string, string>
            {
                { "PORT", portNumber.ToString(CultureInfo.InvariantCulture) },
                { "BROWSER", "none" }, // We don't want create-react-app to open its own extra browser window pointing to the internal dev server port
            };
            var scriptRunner = new NodeScriptRunner(
                sourcePath, scriptName, null, envVars, pkgManagerCommand, diagnosticSource, applicationStoppingToken);
            scriptRunner.AttachToLogger(logger);

            using (var stdErrReader = new EventedStreamStringReader(scriptRunner.StdErr))
            {
                try
                {
                    // Although the React dev server may eventually tell us the URL it's listening on,
                    // it doesn't do so until it's finished compiling, and even then only if there were
                    // no compiler warnings. So instead of waiting for that, consider it ready as soon
                    // as it starts listening for requests.
                    await scriptRunner.StdOut.WaitForMatch(
                        new Regex("Starting the development server", RegexOptions.None, RegexMatchTimeout));
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The {pkgManagerCommand} script '{scriptName}' exited without indicating that the " +
                        $"create-react-app server was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
            }

            return portNumber;
        }
    }
}
