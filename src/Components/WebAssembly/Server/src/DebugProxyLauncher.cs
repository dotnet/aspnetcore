// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

internal static class DebugProxyLauncher
{
    private static readonly object LaunchLock = new object();
    private static readonly TimeSpan DebugProxyLaunchTimeout = TimeSpan.FromSeconds(10);
    private static Task<string>? LaunchedDebugProxyUrl;
    private static readonly Regex NowListeningRegex = new Regex(@"^\s*Now listening on: (?<url>.*)$", RegexOptions.None, TimeSpan.FromSeconds(10));
    private static readonly Regex ApplicationStartedRegex = new Regex(@"^\s*Application started\. Press Ctrl\+C to shut down\.$", RegexOptions.None, TimeSpan.FromSeconds(10));
    private static readonly Regex NowListeningFirefoxRegex = new Regex(@"^\s*Debug proxy for firefox now listening on tcp://(?<url>.*)\. And expecting firefox at port 6000\.$", RegexOptions.None, TimeSpan.FromSeconds(10));
    private static readonly string[] MessageSuppressionPrefixes = new[]
    {
        "Hosting environment:",
        "Content root path:",
        "Now listening on:",
        "Application started. Press Ctrl+C to shut down.",
        "Debug proxy for firefox now",
    };

    public static Task<string> EnsureLaunchedAndGetUrl(IServiceProvider serviceProvider, string devToolsHost, bool isFirefox)
    {
        lock (LaunchLock)
        {
            LaunchedDebugProxyUrl ??= LaunchAndGetUrl(serviceProvider, devToolsHost, isFirefox);

            return LaunchedDebugProxyUrl;
        }
    }

    private static string GetIgnoreProxyForLocalAddress()
    {
        var noProxyEnvVar = Environment.GetEnvironmentVariable("NO_PROXY");
        if (noProxyEnvVar is not null)
        {
            var noProxyEnvVarValues = noProxyEnvVar.Split(",", StringSplitOptions.TrimEntries);
            if (noProxyEnvVarValues.Any(noProxyValue => noProxyValue.Equals("localhost") || noProxyValue.Equals("127.0.0.1")))
            {
                return "--IgnoreProxyForLocalAddress True";
            }
            Console.WriteLine($"Invalid value for NO_PROXY: {noProxyEnvVar} (Expected values: \"localhost\" or \"127.0.0.1\")");
        }
        return "";
    }

    private static async Task<string> LaunchAndGetUrl(IServiceProvider serviceProvider, string devToolsHost, bool isFirefox)
    {
        var tcs = new TaskCompletionSource<string>();

        var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        var executablePath = LocateDebugProxyExecutable(environment);
        var muxerPath = DotNetMuxer.MuxerPathOrDefault();
        var ownerPid = Environment.ProcessId;
        var ignoreProxyForLocalAddress = GetIgnoreProxyForLocalAddress();
        var processStartInfo = new ProcessStartInfo
        {
            FileName = muxerPath,
            Arguments = $"exec \"{executablePath}\" --OwnerPid {ownerPid} --DevToolsUrl {devToolsHost} --IsFirefoxDebugging {isFirefox} --FirefoxProxyPort 6001 {ignoreProxyForLocalAddress}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        RemoveUnwantedEnvironmentVariables(processStartInfo.Environment);

        using var cts = new CancellationTokenSource(DebugProxyLaunchTimeout);
        var ctr = default(CancellationTokenRegistration);
        var debugProxyProcess = Process.Start(processStartInfo);
        if (debugProxyProcess is null)
        {
            tcs.TrySetException(new InvalidOperationException("Unable to start debug proxy process."));
        }
        else
        {
            PassThroughConsoleOutput(debugProxyProcess);
            CompleteTaskWhenServerIsReady(debugProxyProcess, isFirefox, tcs);

            ctr = cts.Token.Register(() =>
            {
                tcs.TrySetException(new TimeoutException($"Failed to start the debug proxy within the timeout period of {DebugProxyLaunchTimeout.TotalSeconds} seconds."));
            });
        }

        try
        {
            return await tcs.Task;
        }
        finally
        {
            ctr.Dispose();
        }
    }

    private static void RemoveUnwantedEnvironmentVariables(IDictionary<string, string?> environment)
    {
        // Generally we expect to pass through most environment variables, since dotnet might
        // need them for arbitrary reasons to function correctly. However, we specifically don't
        // want to pass through any ASP.NET Core hosting related ones, since the child process
        // shouldn't be trying to use the same port numbers, etc. In particular we need to break
        // the association with IISExpress and the MS-ASPNETCORE-TOKEN check.
        // For more context on this, see https://github.com/dotnet/aspnetcore/issues/20308.
        var keysToRemove = environment.Keys.Where(key => key.StartsWith("ASPNETCORE_", StringComparison.Ordinal)).ToList();
        foreach (var key in keysToRemove)
        {
            environment.Remove(key);
        }
    }

    private static string LocateDebugProxyExecutable(IWebHostEnvironment environment)
    {
        if (string.IsNullOrEmpty(environment.ApplicationName))
        {
            throw new InvalidOperationException("IWebHostEnvironment.ApplicationName is required to be set in order to start the debug proxy.");
        }
        var assembly = Assembly.Load(environment.ApplicationName);
        var debugProxyPath = Path.Combine(
            Path.GetDirectoryName(assembly.Location)!,
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
            // It's confusing if the debug proxy emits its own startup status messages, because the developer
            // may think the ports/environment/paths refer to their actual application. So we want to suppress
            // them, but we can't stop the debug proxy app from emitting the messages entirely (e.g., via
            // SuppressStatusMessages) because we need the "Now listening on" one to detect the chosen port.
            // Instead, we'll filter out known strings from the passthrough logic. It's legit to hardcode these
            // strings because they are also hardcoded like this inside WebHostExtensions.cs and can't vary
            // according to culture.
            if (eventArgs.Data is not null)
            {
                foreach (var prefix in MessageSuppressionPrefixes)
                {
                    if (eventArgs.Data.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return;
                    }
                }
            }

            Console.WriteLine(eventArgs.Data);
        };
    }

    private static void CompleteTaskWhenServerIsReady(Process aspNetProcess, bool isFirefox, TaskCompletionSource<string> taskCompletionSource)
    {
        string? capturedUrl = null;
        var errorEncountered = false;

        aspNetProcess.ErrorDataReceived += OnErrorDataReceived;
        aspNetProcess.BeginErrorReadLine();

        aspNetProcess.OutputDataReceived += OnOutputDataReceived;
        aspNetProcess.BeginOutputReadLine();

        void OnErrorDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (!string.IsNullOrEmpty(eventArgs.Data))
            {
                taskCompletionSource.TrySetException(new InvalidOperationException(
                    eventArgs.Data));
                errorEncountered = true;
            }
        }

        void OnOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Data))
            {
                if (!errorEncountered)
                {
                    taskCompletionSource.TrySetException(new InvalidOperationException(
                        "Expected output has not been received from the application."));
                }
                return;
            }

            if (ApplicationStartedRegex.IsMatch(eventArgs.Data) && !isFirefox)
            {
                aspNetProcess.OutputDataReceived -= OnOutputDataReceived;
                aspNetProcess.ErrorDataReceived -= OnErrorDataReceived;
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
                var matchFirefox = NowListeningFirefoxRegex.Match(eventArgs.Data);
                if (matchFirefox.Success && isFirefox)
                {
                    aspNetProcess.OutputDataReceived -= OnOutputDataReceived;
                    aspNetProcess.ErrorDataReceived -= OnErrorDataReceived;
                    capturedUrl = matchFirefox.Groups["url"].Value;
                    taskCompletionSource.TrySetResult(capturedUrl);
                    return;
                }
                var match = NowListeningRegex.Match(eventArgs.Data);
                if (match.Success)
                {
                    capturedUrl = match.Groups["url"].Value;
                    capturedUrl = capturedUrl.Replace("http://", "ws://");
                    capturedUrl = capturedUrl.Replace("https://", "wss://");
                }
            }
        }
    }
}
