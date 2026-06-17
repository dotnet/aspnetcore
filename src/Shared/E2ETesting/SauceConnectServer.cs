// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.Internal;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.E2ETesting;

public class SauceConnectServer : IDisposable
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    private Process _process;
    private string _sentinelPath;
    private Process _sentinelProcess;
    private static IMessageSink _diagnosticsMessageSink;

    // 2h
    private const int SauceConnectProcessTimeout = 7200;

    public SauceConnectServer(IMessageSink diagnosticsMessageSink)
    {
        if (Instance != null || _diagnosticsMessageSink != null)
        {
            throw new InvalidOperationException("Sauce connect singleton already created.");
        }

        // The assembly level attribute AssemblyFixture takes care of this being being instantiated before tests run
        // and disposed after tests are run, gracefully shutting down the server when possible by calling Dispose on
        // the singleton.
        Instance = this;
        _diagnosticsMessageSink = diagnosticsMessageSink;
    }

    private void Initialize(
        Process process,
        string sentinelPath,
        Process sentinelProcess)
    {
        _process = process;
        _sentinelPath = sentinelPath;
        _sentinelProcess = sentinelProcess;
    }

    internal static SauceConnectServer Instance { get; private set; }

    public static async Task StartAsync(ITestOutputHelper output)
    {
        try
        {
            await _semaphore.WaitAsync();
            if (Instance._process == null)
            {
                // No process was started, meaning the instance wasn't initialized.
                await InitializeInstance(output);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task InitializeInstance(ITestOutputHelper output)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "npm",
            Arguments = "run sauce --" +
                $" --sauce-user {E2ETestOptions.Instance.Sauce.Username}" +
                $" --sauce-key {E2ETestOptions.Instance.Sauce.AccessKey}" +
                $" --sauce-tunnel {E2ETestOptions.Instance.Sauce.TunnelIdentifier}" +
                $" --use-hostname {E2ETestOptions.Instance.Sauce.HostName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.FileName = "cmd";
            psi.Arguments = $"/c npm {psi.Arguments}";
        }

        // It's important that we get the folder value before we start the process to prevent
        // untracked processes when the tracking folder is not correctly configure.
        var trackingFolder = GetProcessTrackingFolder();
        if (!Directory.Exists(trackingFolder))
        {
            throw new InvalidOperationException($"Invalid tracking folder. Set the 'SauceConnectProcessTrackingFolder' MSBuild property to a valid folder.");
        }

        Process process = null;
        Process sentinel = null;
        string pidFilePath = null;
        try
        {
            process = Process.Start(psi);
            pidFilePath = await WriteTrackingFileAsync(output, trackingFolder, process);
            sentinel = StartSentinelProcess(process, pidFilePath, SauceConnectProcessTimeout);
        }
        catch
        {
            ProcessCleanup(process, pidFilePath);
            ProcessCleanup(sentinel, pidFilePath: null);
            throw;
        }

        // Log output for sauce connect process.
        // This is for the case where the server fails to launch.
        var logOutput = new BlockingCollection<string>();

        process.OutputDataReceived += LogOutput;
        process.ErrorDataReceived += LogOutput;

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // The Sauce connect server has to be up for the entirety of the tests and is only shutdown when the application (i.e. the test) exits.
        AppDomain.CurrentDomain.ProcessExit += (sender, args) => ProcessCleanup(process, pidFilePath);

        // Log
        void LogOutput(object sender, DataReceivedEventArgs e)
        {
            logOutput.TryAdd(e.Data);

            // We avoid logging on the output here because it is unreliable. We can only log in the diagnostics sink.
            lock (_diagnosticsMessageSink)
            {
                _diagnosticsMessageSink.OnMessage(new DiagnosticMessage(e.Data));
            }
        }

        var uri = new UriBuilder("http", E2ETestOptions.Instance.Sauce.HostName, 4445).Uri;
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(1),
        };

        var retries = 0;
        do
        {
            await Task.Delay(1000);
            try
            {
                var response = await httpClient.GetAsync(uri);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    output = null;
                    Instance.Initialize(process, pidFilePath, sentinel);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpRequestException)
            {
            }

            retries++;
        } while (retries < 30);

        // Make output null so that we stop logging to it.
        output = null;
        logOutput.CompleteAdding();
        var exitCodeString = process.HasExited ? process.ExitCode.ToString(CultureInfo.InvariantCulture) : "Process has not yet exited.";
        var message = $@"Failed to launch the server.
ExitCode: {exitCodeString}
Captured output lines:
{string.Join(Environment.NewLine, logOutput.GetConsumingEnumerable())}.";

        // If we got here, we couldn't launch Sauce connect or get it to respond. So shut it down.
        ProcessCleanup(process, pidFilePath);
        throw new InvalidOperationException(message);
    }

    private static Process StartSentinelProcess(Process process, string sentinelFile, int timeout)
    {
        // This sentinel process will start and will kill any rouge sauce connect server that wasn't torn down via normal means.
        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -NonInteractive -Command \"Start-Sleep {timeout}; " +
            $"if(Test-Path {sentinelFile}){{ " +
            $"Write-Output 'Stopping process {process.Id}'; Stop-Process {process.Id}; }}" +
            $"else{{ Write-Output 'Sentinel file {sentinelFile} not found.'}}",
        };

        return Process.Start(psi);
    }

    private static void ProcessCleanup(Process process, string pidFilePath)
    {
        try
        {
            if (process?.HasExited == false)
            {
                try
                {
                    process?.KillTree(TimeSpan.FromSeconds(10));
                    process?.Dispose();
                }
                catch
                {
                    // Ignore errors here since we can't do anything
                }
            }
            if (pidFilePath != null && File.Exists(pidFilePath))
            {
                File.Delete(pidFilePath);
            }
        }
        catch
        {
            // Ignore errors here since we can't do anything
        }
    }

    private static async Task<string> WriteTrackingFileAsync(ITestOutputHelper output, string trackingFolder, Process process)
    {
        var pidFile = Path.Combine(trackingFolder, $"{process.Id}.{Guid.NewGuid()}.pid");
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await File.WriteAllTextAsync(pidFile, process.Id.ToString(CultureInfo.InvariantCulture));
                return pidFile;
            }
            catch
            {
                output.WriteLine($"Can't write file to process tracking folder: {trackingFolder}");
            }
        }

        throw new InvalidOperationException($"Failed to write file for process {process.Id}");
    }

    private static string GetProcessTrackingFolder() =>
        typeof(SauceConnectServer).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "Microsoft.AspNetCore.InternalTesting.SauceConnect.ProcessTracking").Value;

    public void Dispose()
    {
        ProcessCleanup(_process, _sentinelPath);
        ProcessCleanup(_sentinelProcess, pidFilePath: null);
    }
}
