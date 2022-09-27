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
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.Internal;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.E2ETesting;

public class SeleniumStandaloneServer : IDisposable
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    private Process _process;
    private string _sentinelPath;
    private Process _sentinelProcess;
    private static IMessageSink _diagnosticsMessageSink;

    // 1h 30 min
    private const int SeleniumProcessTimeout = 3600;

    public SeleniumStandaloneServer(IMessageSink diagnosticsMessageSink)
    {
        if (Instance != null || _diagnosticsMessageSink != null)
        {
            throw new InvalidOperationException("Selenium standalone singleton already created.");
        }

        // The assembly level attribute AssemblyFixture takes care of this being being instantiated before tests run
        // and disposed after tests are run, gracefully shutting down the server when possible by calling Dispose on
        // the singleton.
        Instance = this;
        _diagnosticsMessageSink = diagnosticsMessageSink;
    }

    private void Initialize(
        Uri uri,
        Process process,
        string sentinelPath,
        Process sentinelProcess)
    {
        Uri = uri;
        _process = process;
        _sentinelPath = sentinelPath;
        _sentinelProcess = sentinelProcess;
    }

    public Uri Uri { get; private set; }

    internal static SeleniumStandaloneServer Instance { get; private set; }

    public static async Task<SeleniumStandaloneServer> GetInstanceAsync(ITestOutputHelper output)
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

        return Instance;
    }

    private static async Task InitializeInstance(ITestOutputHelper output)
    {
        var port = FindAvailablePort();
        var uri = new UriBuilder("http", "localhost", port, "/wd/hub").Uri;

        var seleniumConfigPath = typeof(SeleniumStandaloneServer).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(k => k.Key == "Microsoft.AspNetCore.Testing.SeleniumConfigPath")
            ?.Value;

        if (seleniumConfigPath == null)
        {
            throw new InvalidOperationException("Selenium config path not configured. Does this project import the E2ETesting.targets?");
        }

        // In AzDO, the path to the system chromedriver is in an env var called CHROMEWEBDRIVER
        // We want to use this because it should match the installed browser version
        // If the env var is not set, then we fall back on using whatever is in the Selenium config file
        var chromeDriverArg = string.Empty;
        var chromeDriverPathEnvVar = Environment.GetEnvironmentVariable("CHROMEWEBDRIVER");
        if (!string.IsNullOrEmpty(chromeDriverPathEnvVar))
        {
            chromeDriverArg = $"--javaArgs=-Dwebdriver.chrome.driver={chromeDriverPathEnvVar}/chromedriver";
            output.WriteLine($"Using chromedriver at path {chromeDriverPathEnvVar}");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "npm",
            Arguments = $"run selenium-standalone start -- --config \"{seleniumConfigPath}\" {chromeDriverArg} -- -port {port}",
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
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("helix")))
        {
            // Just create a random tracking folder on helix
            trackingFolder = Path.Combine(Directory.GetCurrentDirectory(), Path.GetRandomFileName());
            Directory.CreateDirectory(trackingFolder);
        }

        if (!Directory.Exists(trackingFolder))
        {
            throw new InvalidOperationException($"Invalid tracking folder. Set the 'SeleniumProcessTrackingFolder' MSBuild property to a valid folder.");
        }

        Process process = null;
        Process sentinel = null;
        string pidFilePath = null;
        try
        {
            process = Process.Start(psi);
            pidFilePath = await WriteTrackingFileAsync(output, trackingFolder, process);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                sentinel = StartSentinelProcess(process, pidFilePath, SeleniumProcessTimeout);
            }
        }
        catch
        {
            ProcessCleanup(process, pidFilePath);

            if (sentinel is not null)
            {
                ProcessCleanup(sentinel, pidFilePath: null);
            }

            throw;
        }

        // Log output for selenium standalone process.
        // This is for the case where the server fails to launch.
        var logOutput = new BlockingCollection<string>();

        process.OutputDataReceived += LogOutput;
        process.ErrorDataReceived += LogOutput;

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // The Selenium sever has to be up for the entirety of the tests and is only shutdown when the application (i.e. the test) exits.
        // AppDomain.CurrentDomain.ProcessExit += (sender, args) => ProcessCleanup(process, pidFilePath);

        // Log
        void LogOutput(object sender, DataReceivedEventArgs e)
        {
            try
            {
                logOutput.TryAdd(e.Data);
            }
            catch (Exception)
            {
            }

            // We avoid logging on the output here because it is unreliable. We can only log in the diagnostics sink.
            lock (_diagnosticsMessageSink)
            {
                _diagnosticsMessageSink.OnMessage(new DiagnosticMessage(e.Data));
            }
        }

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
                    Instance.Initialize(uri, process, pidFilePath, sentinel);
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

        // If we got here, we couldn't launch Selenium or get it to respond. So shut it down.
        ProcessCleanup(process, pidFilePath);
        throw new InvalidOperationException(message);
    }

    private static Process StartSentinelProcess(Process process, string sentinelFile, int timeout)
    {
        // This sentinel process will start and will kill any rouge selenium server that want' torn down
        // via normal means.
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

    static int FindAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);

        try
        {
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string GetProcessTrackingFolder() =>
        typeof(SeleniumStandaloneServer).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "Microsoft.AspNetCore.Testing.Selenium.ProcessTracking").Value;

    public void Dispose()
    {
        ProcessCleanup(_process, _sentinelPath);

        if (_sentinelProcess is not null)
        {
            ProcessCleanup(_sentinelProcess, pidFilePath: null);
        }
    }
}
