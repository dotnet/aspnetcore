// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    internal static class DebugProxyLauncher
    {
        private static readonly object LaunchLock = new object();
        private static readonly TimeSpan DebugProxyLaunchTimeout = TimeSpan.FromSeconds(10);
        private static Task<string> LaunchedDebugProxyUrl;
        private static readonly Regex NowListeningRegex = new Regex(@"^\s*Now listening on: (?<url>.*)$", RegexOptions.None, TimeSpan.FromSeconds(10));
        private static readonly Regex ApplicationStartedRegex = new Regex(@"^\s*Application started\. Press Ctrl\+C to shut down\.$", RegexOptions.None, TimeSpan.FromSeconds(10));

        public static Task<string> EnsureLaunchedAndGetUrl(IServiceProvider serviceProvider)
        {
            lock (LaunchLock)
            {
                if (LaunchedDebugProxyUrl == null)
                {
                    LaunchedDebugProxyUrl = LaunchAndGetUrl(serviceProvider);
                }

                return LaunchedDebugProxyUrl;
            }
        }

        private static async Task<string> LaunchAndGetUrl(IServiceProvider serviceProvider)
        {
            var tcs = new TaskCompletionSource<string>();

            var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var executablePath = LocateDebugProxyExecutable(environment);
            var muxerPath = DotNetMuxer.MuxerPathOrDefault();
            var ownerPid = Process.GetCurrentProcess().Id;
            var processStartInfo = new ProcessStartInfo
            {
                FileName = muxerPath,
                Arguments = $"exec \"{executablePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            RemoveUnwantedEnvironmentVariables(processStartInfo.Environment);

            var debugProxyProcess = Process.Start(processStartInfo);
            PassThroughConsoleOutput(debugProxyProcess);
            CompleteTaskWhenServerIsReady(debugProxyProcess, tcs);

            new CancellationTokenSource(DebugProxyLaunchTimeout).Token.Register(() =>
            {
                tcs.TrySetException(new TimeoutException($"Failed to start the debug proxy within the timeout period of {DebugProxyLaunchTimeout.TotalSeconds} seconds."));
            });

            return await tcs.Task;
        }

        private static void RemoveUnwantedEnvironmentVariables(IDictionary<string, string> environment)
        {
            // Generally we expect to pass through most environment variables, since dotnet might
            // need them for arbitrary reasons to function correctly. However, we specifically don't
            // want to pass through any ASP.NET Core hosting related ones, since the child process
            // shouldn't be trying to use the same port numbers, etc. In particular we need to break
            // the association with IISExpress and the MS-ASPNETCORE-TOKEN check.
            // For more context on this, see https://github.com/dotnet/aspnetcore/issues/20308.
            var keysToRemove = environment.Keys.Where(key => key.StartsWith("ASPNETCORE_")).ToList();
            foreach (var key in keysToRemove)
            {
                environment.Remove(key);
            }
        }

        private static string LocateDebugProxyExecutable(IWebHostEnvironment environment)
        {
            var assembly = Assembly.Load(environment.ApplicationName);
            var debugProxyPath = Path.Combine(
                Path.GetDirectoryName(assembly.Location),
                "BlazorDebugProxy",
                "BrowserDebugHost.dll");

            if (!File.Exists(debugProxyPath))
            {
                throw new FileNotFoundException(
                    $"Cannot start debug proxy because it cannot be found at '{debugProxyPath}'");
            }

            return debugProxyPath;
        }

        private static void PassThroughConsoleOutput(Process process)
        {
            process.OutputDataReceived += (sender, eventArgs) =>
            {
                Console.WriteLine(eventArgs.Data);
            };
        }

        private static void CompleteTaskWhenServerIsReady(Process aspNetProcess, TaskCompletionSource<string> taskCompletionSource)
        {
            string capturedUrl = null;
            aspNetProcess.OutputDataReceived += OnOutputDataReceived;
            aspNetProcess.BeginOutputReadLine();

            void OnOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
            {
                if (ApplicationStartedRegex.IsMatch(eventArgs.Data))
                {
                    aspNetProcess.OutputDataReceived -= OnOutputDataReceived;
                    if (!string.IsNullOrEmpty(capturedUrl))
                    {
                        taskCompletionSource.TrySetResult(capturedUrl);
                    }
                    else
                    {
                        taskCompletionSource.TrySetException(new InvalidOperationException(
                            "The application started listening without first advertising a URL"));
                    }
                }
                else
                {
                    var match = NowListeningRegex.Match(eventArgs.Data);
                    if (match.Success)
                    {
                        capturedUrl = match.Groups["url"].Value;
                    }
                }
            }
        }
    }
}
