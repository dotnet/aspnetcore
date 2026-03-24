// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Represents a running app instance started via
/// <see cref="ServerFixture{TTestAssembly}.StartServerAsync{TApp}"/>.
/// Each instance has a unique <see cref="Id"/> used for YARP proxy routing
/// via the <c>X-Test-Backend</c> header.
/// </summary>
/// <remarks>
/// Disposing a <see cref="ServerInstance"/> kills the app process and unregisters
/// it from the proxy. Instances are typically disposed by the <see cref="ServerFixture{TTestAssembly}"/>
/// when the collection is torn down, but tests can also dispose them early for
/// explicit lifecycle control.
/// </remarks>
public class ServerInstance : IAsyncDisposable
{
    private Process? _process;
    private readonly string _proxyUrl;
    private readonly Action<string>? _onDisposed;
    private readonly StringBuilder _stdoutBuffer = new();
    private readonly StringBuilder _stderrBuffer = new();

    /// <summary>
    /// Unique identifier for this server instance (used for <c>X-Test-Backend</c> header).
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// The app name (matches the key in the E2E manifest).
    /// </summary>
    public string AppName { get; }

    /// <summary>
    /// Direct URL of the app process (random port, localhost).
    /// </summary>
    public string AppUrl { get; private set; } = "";

    /// <summary>
    /// Public-facing URL from the manifest (for OAuth redirect URIs, etc.).
    /// </summary>
    public string? PublicUrl { get; private set; }

    /// <summary>
    /// URL that tests should navigate to. Always routes through the proxy.
    /// Tests must set <c>ExtraHTTPHeaders["X-Test-Backend"] = Id</c> to reach this instance.
    /// </summary>
    public string TestUrl => _proxyUrl;

    internal string Key { get; }

    internal ServerInstance(string appName, string key, string proxyUrl, Action<string>? onDisposed)
    {
        AppName = appName;
        Key = key;
        _proxyUrl = proxyUrl;
        _onDisposed = onDisposed;
    }

    internal async Task LaunchAsync(
        E2EAppEntry appEntry,
        ServerStartOptions options,
        string testAssemblyLocation,
        string testAssemblyName,
        string readyUrl,
        Task readySignal)
    {
        var port = GetAvailablePort();
        AppUrl = $"http://localhost:{port}";
        PublicUrl = appEntry.PublicUrl;

        ProcessStartInfo startInfo;

        if (appEntry.Published is not null)
        {
            startInfo = CreatePublishedStartInfo(appEntry);
        }
        else if (appEntry.ProjectPath is not null)
        {
            startInfo = new ProcessStartInfo("dotnet",
                $"run --no-launch-profile --project \"{appEntry.ProjectPath}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
        }
        else
        {
            throw new InvalidOperationException(
                $"App '{AppName}' has no projectPath or published info in the manifest.");
        }

        // Inject E2E test infrastructure
        startInfo.Environment["ASPNETCORE_URLS"] = AppUrl;
        startInfo.Environment["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = testAssemblyName;
        startInfo.Environment["DOTNET_STARTUP_HOOKS"] = testAssemblyLocation;
        startInfo.Environment["TEST_PARENT_PID"] = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        // Propagate DOTNET_ROOT so published app hosts can find the correct runtime
        // (e.g. when building against a preview SDK that isn't globally installed).
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(dotnetRoot))
        {
            startInfo.Environment["DOTNET_ROOT"] = dotnetRoot;
        }

        // Readiness callback: the app POSTs to this URL when ApplicationStarted fires
        startInfo.Environment["E2E_READY_URL"] = readyUrl;

        // Service override: static method pattern (WAF-like)
        if (options.ServiceOverrideTypeName is not null)
        {
            startInfo.Environment["E2E_TEST_SERVICES_TYPE"] = options.ServiceOverrideTypeName;
            startInfo.Environment["E2E_TEST_SERVICES_METHOD"] = options.ServiceOverrideMethodName;
        }

        // Manifest-defined environment variables
        foreach (var (key, value) in appEntry.EnvironmentVariables)
        {
            startInfo.Environment[key] = value;
        }

        // User-specified environment variables (override manifest ones)
        foreach (var (key, value) in options.EnvironmentVariables)
        {
            startInfo.Environment[key] = value;
        }

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process for '{AppName}'.");

        _ = DrainStreamAsync(_process.StandardOutput, $"[{AppName}:{Id}] ", _stdoutBuffer);
        _ = DrainStreamAsync(_process.StandardError, $"[{AppName}:{Id} ERR] ", _stderrBuffer);

        // Wait for the app to signal readiness via the callback, with timeout as fallback.
        // Also monitor for early process exit to fail fast with diagnostics.
        var timeoutTask = Task.Delay(options.ReadinessTimeoutMs);
        var processExitTask = _process.WaitForExitAsync();
        var completed = await Task.WhenAny(readySignal, timeoutTask, processExitTask).ConfigureAwait(false);

        if (completed == processExitTask)
        {
            // Give drain tasks a moment to capture remaining output
            await Task.Delay(100).ConfigureAwait(false);
            var output = GetCapturedOutput();
            throw new InvalidOperationException(
                $"App '{AppName}' process exited with code {_process.ExitCode} before signaling readiness. " +
                $"Project: {appEntry.ProjectPath ?? "(published)"}.\n" +
                $"--- Captured output ---\n{output}");
        }

        if (completed == timeoutTask)
        {
            var output = GetCapturedOutput();
            throw new TimeoutException(
                $"App '{AppName}' did not signal readiness within {options.ReadinessTimeoutMs}ms. " +
                $"Process still running: {!_process.HasExited}. " +
                $"Project: {appEntry.ProjectPath ?? "(published)"}.\n" +
                $"--- Captured output ---\n{output}");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _onDisposed?.Invoke(Key);

        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync().ConfigureAwait(false);
        }

        _process?.Dispose();
    }

    internal static string ComputeKey(string appName, ServerStartOptions options)
    {
        var sb = new StringBuilder(appName);

        if (options.ServiceOverrideTypeName is not null)
        {
            sb.Append('|').Append(options.ServiceOverrideTypeName);
            sb.Append(':').Append(options.ServiceOverrideMethodName);
        }

        foreach (var kvp in options.EnvironmentVariables.OrderBy(x => x.Key))
        {
            sb.Append('|').Append(kvp.Key).Append('=').Append(kvp.Value);
        }

        return sb.ToString();
    }

    static ProcessStartInfo CreatePublishedStartInfo(E2EAppEntry appEntry)
    {
        var published = appEntry.Published!;
        var e2eAppsDir = Path.Combine(AppContext.BaseDirectory, "e2e-apps");
        var workingDir = Path.Combine(e2eAppsDir, published.WorkingDirectory);

        var fileName = published.Executable == "dotnet"
            ? "dotnet"
            : Path.Combine(workingDir, published.Executable);

        return new ProcessStartInfo(fileName, published.Args)
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
    }

    static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    static async Task DrainStreamAsync(StreamReader reader, string prefix, StringBuilder buffer)
    {
        try
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                lock (buffer)
                {
                    buffer.AppendLine(line);
                }
                Console.WriteLine($"{prefix}{line}");
            }
        }
        catch
        {
            // Process exited
        }
    }

    string GetCapturedOutput()
    {
        var sb = new StringBuilder();
        lock (_stdoutBuffer)
        {
            if (_stdoutBuffer.Length > 0)
            {
                sb.AppendLine("[STDOUT]");
                sb.Append(_stdoutBuffer);
            }
        }
        lock (_stderrBuffer)
        {
            if (_stderrBuffer.Length > 0)
            {
                sb.AppendLine("[STDERR]");
                sb.Append(_stderrBuffer);
            }
        }
        return sb.Length > 0 ? sb.ToString() : "(no output captured)";
    }
}
