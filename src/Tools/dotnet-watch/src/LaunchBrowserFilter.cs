// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Tools
{
    public sealed class LaunchBrowserFilter : IWatchFilter, IAsyncDisposable
    {
        private readonly byte[] ReloadMessage = Encoding.UTF8.GetBytes("Reload");
        private readonly byte[] WaitMessage = Encoding.UTF8.GetBytes("Wait");
        private static readonly Regex NowListeningRegex = new Regex(@"^\s*Now listening on: (?<url>.*)$", RegexOptions.None | RegexOptions.Compiled, TimeSpan.FromSeconds(10));
        private readonly bool _runningInTest;
        private readonly bool _suppressLaunchBrowser;
        private readonly bool _suppressBrowserRefresh;
        private readonly string _browserPath;

        private bool _canLaunchBrowser;
        private Process _browserProcess;
        private bool _browserLaunched;
        private BrowserRefreshServer _refreshServer;
        private IReporter _reporter;
        private string _launchPath;
        private CancellationToken _cancellationToken;

        public LaunchBrowserFilter()
        {
            var suppressLaunchBrowser = Environment.GetEnvironmentVariable("DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER");
            _suppressLaunchBrowser = (suppressLaunchBrowser == "1" || suppressLaunchBrowser == "true");

            var suppressBrowserRefresh = Environment.GetEnvironmentVariable("DOTNET_WATCH_SUPPRESS_BROWSER_REFRESH");
            _suppressBrowserRefresh = (suppressBrowserRefresh == "1" || suppressBrowserRefresh == "true");

            _runningInTest = Environment.GetEnvironmentVariable("__DOTNET_WATCH_RUNNING_AS_TEST") == "true";
            _browserPath = Environment.GetEnvironmentVariable("DOTNET_WATCH_BROWSER_PATH");
        }

        public async ValueTask ProcessAsync(DotNetWatchContext context, CancellationToken cancellationToken)
        {
            if (_suppressLaunchBrowser)
            {
                return;
            }

            if (context.Iteration == 0)
            {
                _reporter = context.Reporter;

                if (CanLaunchBrowser(context, out var launchPath))
                {
                    context.Reporter.Verbose("dotnet-watch is configured to launch a browser on ASP.NET Core application startup.");
                    _canLaunchBrowser = true;
                    _launchPath = launchPath;
                    _cancellationToken = cancellationToken;

                    // We've redirected the output, but want to ensure that it continues to appear in the user's console.
                    context.ProcessSpec.OnOutput += (_, eventArgs) => Console.WriteLine(eventArgs.Data);
                    context.ProcessSpec.OnOutput += OnOutput;

                    if (!_suppressBrowserRefresh)
                    {
                        _refreshServer = new BrowserRefreshServer(context.Reporter);
                        var serverUrl = await _refreshServer.StartAsync(cancellationToken);

                        context.Reporter.Verbose($"Refresh server running at {serverUrl}.");
                        context.ProcessSpec.EnvironmentVariables["ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT"] = serverUrl;

                        var pathToMiddleware = Path.Combine(AppContext.BaseDirectory, "middleware", "Microsoft.AspNetCore.Watch.BrowserRefresh.dll");
                        context.ProcessSpec.EnvironmentVariables["DOTNET_STARTUP_HOOKS"] = pathToMiddleware;
                        context.ProcessSpec.EnvironmentVariables["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Microsoft.AspNetCore.Watch.BrowserRefresh";
                    }
                }
            }

            if (_canLaunchBrowser)
            {
                if (context.Iteration > 0)
                {
                    // We've detected a change. Notify the browser.
                    await SendMessage(WaitMessage, cancellationToken);
                }
            }
        }

        private Task SendMessage(byte[] message, CancellationToken cancellationToken)
        {
            if (_refreshServer is null)
            {
                return Task.CompletedTask;
            }

            return _refreshServer.SendMessage(message, cancellationToken);
        }

        private void OnOutput(object sender, DataReceivedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Data))
            {
                return;
            }

            var match = NowListeningRegex.Match(eventArgs.Data);
            if (match.Success)
            {
                var launchUrl = match.Groups["url"].Value;

                var process = (Process)sender;
                process.OutputDataReceived -= OnOutput;

                if (!_browserLaunched)
                {
                    _reporter.Verbose("Launching browser.");
                    try
                    {
                        LaunchBrowser(launchUrl);
                        _browserLaunched = true;
                    }
                    catch (Exception ex)
                    {
                        _reporter.Output($"Unable to launch browser: {ex}");
                        _canLaunchBrowser = false;
                    }
                }
                else
                {
                    _reporter.Verbose("Reloading browser.");
                    _ = SendMessage(ReloadMessage, _cancellationToken);
                }
            }
        }

        private void LaunchBrowser(string launchUrl)
        {
            var fileName = launchUrl + "/" + _launchPath;
            var args = string.Empty;
            if (!string.IsNullOrEmpty(_browserPath))
            {
                args = fileName;
                fileName = _browserPath;
            }

            if (_runningInTest)
            {
                _reporter.Output($"Launching browser: {fileName} {args}");
                return;
            }

            _browserProcess = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = true,
            });
        }

        private static bool CanLaunchBrowser(DotNetWatchContext context, out string launchUrl)
        {
            launchUrl = null;
            var reporter = context.Reporter;

            if (!context.FileSet.IsNetCoreApp31OrNewer)
            {
                // Browser refresh middleware supports 3.1 or newer
                reporter.Verbose("Browser refresh is only supported in .NET Core 3.1 or newer projects.");
                return false;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Launching a browser requires file associations that are not available in all operating systems.
                reporter.Verbose("Browser refresh is only supported in Windows and MacOS.");
                return false;
            }

            var dotnetCommand = context.ProcessSpec.Arguments.FirstOrDefault();
            if (!string.Equals(dotnetCommand, "run", StringComparison.Ordinal))
            {
                reporter.Verbose("Browser refresh is only supported for run commands.");
                return false;
            }

            // We're executing the run-command. Determine if the launchSettings allows it
            var launchSettingsPath = Path.Combine(context.ProcessSpec.WorkingDirectory, "Properties", "launchSettings.json");
            if (!File.Exists(launchSettingsPath))
            {
                reporter.Verbose($"No launchSettings.json file found at {launchSettingsPath}. Unable to determine if browser refresh is allowed.");
                return false;
            }

            LaunchSettingsJson launchSettings;
            try
            {
                launchSettings = JsonSerializer.Deserialize<LaunchSettingsJson>(
                    File.ReadAllText(launchSettingsPath),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (Exception ex)
            {
                reporter.Verbose($"Error reading launchSettings.json: {ex}.");
                return false;
            }

            var defaultProfile = launchSettings.Profiles.FirstOrDefault(f => f.Value.CommandName == "Project").Value;
            if (defaultProfile is null)
            {
                reporter.Verbose("Unable to find default launchSettings profile.");
                return false;
            }

            if (!defaultProfile.LaunchBrowser)
            {
                reporter.Verbose("launchSettings does not allow launching browsers.");
                return false;
            }

            launchUrl = defaultProfile.LaunchUrl;
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            _browserProcess?.Dispose();
            if (_refreshServer != null)
            {
                await _refreshServer.DisposeAsync();
            }
        }
    }
}
