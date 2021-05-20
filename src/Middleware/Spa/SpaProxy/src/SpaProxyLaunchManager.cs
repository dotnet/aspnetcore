// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SpaProxy
{
    internal class SpaProxyLaunchManager : IHostedService, IDisposable
    {
        private readonly SpaDevelopmentServerOptions _options;
        private readonly ILogger<SpaProxyLaunchManager> _logger;

        private Process? _spaProcess;
        private bool _disposedValue;

        public SpaProxyLaunchManager(ILogger<SpaProxyLaunchManager> logger)
        {
            _options = new SpaDevelopmentServerOptions();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "spa.proxy.json"))
                .Build();
            configuration.GetSection("SpaProxyServer").Bind(_options);
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var httpClient = new HttpClient(new HttpClientHandler()
            {
                // It's ok for us to do this here since this service is only plugged in during development.
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            _logger.LogInformation("Starting SPA development server");
            var running = await ProbeSpaDevelopmentServerUrl(httpClient, cancellationToken);
            if (running)
            {
                _logger.LogInformation($"Found SPA development server running at {_options.ServerUrl}");
            }
            else
            {
                _logger.LogInformation($"No SPA development server running at {_options.ServerUrl} found.");
                await StartSpaProcessAndProbeForLiveness(httpClient, cancellationToken);
            }
        }

        private async Task<bool> ProbeSpaDevelopmentServerUrl(HttpClient httpClient, CancellationToken cancellationToken)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
            try
            {
                var response = await httpClient.GetAsync(_options.ServerUrl, cancellationTokenSource.Token);
                var running = response.IsSuccessStatusCode;
                return running;
            }
            catch (Exception exception) when (exception is HttpRequestException ||
                  exception is TaskCanceledException ||
                  exception is OperationCanceledException)
            {
                _logger.LogDebug(exception, "Failed to connect to the SPA Development proxy.");
                return false;
            }
        }

        private async Task StartSpaProcessAndProbeForLiveness(HttpClient httpClient, CancellationToken cancellationToken)
        {
            LaunchDevelopmentProxy();
            var sw = Stopwatch.StartNew();
            var livenessProbeSucceeded = false;
            var maxTimeoutReached = false;
            while (_spaProcess != null && !_spaProcess.HasExited && !maxTimeoutReached)
            {
                livenessProbeSucceeded = await ProbeSpaDevelopmentServerUrl(httpClient, cancellationToken);
                if (livenessProbeSucceeded)
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                maxTimeoutReached = sw.Elapsed >= _options.MaxTimeout;
                await Task.Delay(1000, cancellationToken);
            }

            if (_spaProcess == null || _spaProcess.HasExited)
            {
                _logger.LogError($"Couldn't start the SPA development server with command '{_options.LaunchCommand}'.");
            }
            else if (!livenessProbeSucceeded)
            {
                _logger.LogError($"Unable to connect to the SPA development server at '{_options.ServerUrl}'.");
            }
            else
            {
                _logger.LogInformation($"SPA development server running at '{_options.ServerUrl}'");
            }
        }

        private void LaunchDevelopmentProxy()
        {
            try
            {
                // Launch command is going to be something like `npm/yarn <<verb>> <<options>>`
                // We split it into two to separate the tool (command) from the verb and the rest of the arguments.
                var space = _options.LaunchCommand.IndexOf(' ');
                var command = _options.LaunchCommand[0..space];
                var arguments = _options.LaunchCommand[++space..];
                if (OperatingSystem.IsWindows() && !Path.HasExtension(command))
                {
                    // On windows we transform npm/yarn to npm.cmd/yarn.cmd so that the command
                    // can actually be found when we start the process. This is overridable if
                    // necessary by explicitly setting up the extension on the command.
                    command = $"{command}.cmd";
                }

                var info = new ProcessStartInfo(command, arguments)
                {
                    // Linux and Mac OS don't have the concept of launching a terminal process in a new window.
                    // On those cases the process will be launched in the same terminal window and will just print
                    // some output during the start phase of the app.
                    // This is not a problem since users don't need to interact with the proxy other than to stop it
                    // and this is only an optimization to keep the current experience. We can always tell them to
                    // run the proxy manually.
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = Path.Combine(AppContext.BaseDirectory, _options.WorkingDirectory)
                };
                _spaProcess = Process.Start(info);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Failed to launch the SPA development server '{_options.LaunchCommand}'.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // We don't need to do anything here since Dispose will take care of cleaning up the process if necessary.
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Nothing to do here since ther are no managed resources
                }

                try
                {
                    if (_spaProcess != null && !_spaProcess.HasExited)
                    {
                        // Review: Whether or not to do this at all. Turns out that if we try to kill the
                        // npm.cmd/ps1 process that we start, even with this option we only stop this process
                        // and the service keeps running.
                        // Compared to performing Ctrl+C on the window or closing the window for the newly spawned
                        // process which seems to do the right thing.
                        // Process.CloseMainWindow seems to do the right thing in this situation and is doable since
                        // we now start a proxy every time.
                        // We can't guarantee that we stop/cleanup the proxy on every situation (for example if someone)
                        // kills this process in a "rude" way, but this gets 95% there.
                        // For cases where the proxy is left open and where there might not be a "visible" window the recomendation
                        // is to kill the process manually. (We will not fail, we will simply notify the proxy is "already" up.
                        if (!_spaProcess.CloseMainWindow())
                        {
                            _spaProcess.Kill(entireProcessTree: true);
                            _spaProcess = null;
                        }
                    }
                }
                catch (Exception)
                {
                    // Avoid throwing if we are running inside the finalizer.
                    if (disposing)
                    {
                        throw;
                    }
                }

                _disposedValue = true;
            }
        }

        ~SpaProxyLaunchManager()
        {
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private class SpaDevelopmentServerOptions
        {
            public string ServerUrl { get; set; } = "";

            public string LaunchCommand { get; set; } = "";

            public int MaxTimeoutInSeconds { get; set; }

            public TimeSpan MaxTimeout => TimeSpan.FromSeconds(MaxTimeoutInSeconds);

            public string WorkingDirectory { get; set; } = "";
        }
    }
}
