// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Installs the locally-built <c>Microsoft.AspNetCore.Components.Gateway.Cli</c> package as a dotnet
/// tool into a throwaway tool-path and lets tests launch the <c>blazor-gateway</c> command from it.
/// The working folder lives under the repo's artifacts/tmp directory and is removed on dispose.
/// </summary>
internal sealed partial class GatewayToolInstallation : IDisposable
{
    private readonly string _root;
    private readonly StringBuilder _installLog = new();

    private GatewayToolInstallation(string root)
    {
        _root = root;
    }

    public string ToolPath => Path.Combine(_root, "tools");

    public string CommandPath
    {
        get
        {
            var fileName = OperatingSystem.IsWindows()
                ? $"{GatewayCliTestData.ToolCommandName}.exe"
                : GatewayCliTestData.ToolCommandName;
            return Path.Combine(ToolPath, fileName);
        }
    }

    /// <summary>
    /// The directory under the tool-path's NuGet fallback folder (<c>.store</c>) that holds the
    /// installed package's unpacked contents, mirroring the layout of the original .nupkg.
    /// </summary>
    public string PackageContentPath
    {
        get
        {
            var packageId = GatewayCliTestData.PackageId.ToLowerInvariant();
            var version = GatewayCliTestData.PackageVersion.ToLowerInvariant();
            return Path.Combine(ToolPath, ".store", packageId, version, packageId, version);
        }
    }

    /// <summary>
    /// Checks whether the installed package contains the given file, addressed relative to
    /// <see cref="PackageContentPath"/>.
    /// </summary>
    public bool HasFile(string relativePath)
        => File.Exists(Path.Combine(PackageContentPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>
    /// Reads the contents of a file installed from the package, addressed relative to
    /// <see cref="PackageContentPath"/>.
    /// </summary>
    public string ReadFile(string relativePath)
    {
        var path = Path.Combine(PackageContentPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"File '{relativePath}' was not found in the installed '{GatewayCliTestData.PackageId}' tool at '{PackageContentPath}'.");
        }

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Reads the .nuspec that NuGet keeps alongside the installed package's unpacked contents.
    /// </summary>
    public string ReadNuspec() => ReadFile($"{GatewayCliTestData.PackageId}.nuspec");

    /// <summary>
    /// Installs the tool into a fresh tool-path from the local package output. Callers must gate the
    /// test with <see cref="RequiresBuiltGatewayCliPackageAttribute"/>.
    /// </summary>
    public static GatewayToolInstallation Install()
    {
        var root = Path.Combine(
            GatewayCliTestData.ArtifactsTmpDir,
            "GatewayCliToolTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var installation = new GatewayToolInstallation(root);

        var arguments = new StringBuilder()
            .Append("tool install ").Append(GatewayCliTestData.PackageId)
            .Append(" --tool-path \"").Append(installation.ToolPath).Append('"')
            .Append(" --version ").Append(GatewayCliTestData.PackageVersion)
            .Append(" --add-source \"").Append(GatewayCliTestData.ShippingPackagesDir.TrimEnd('\\', '/')).Append('"');

        if (!string.IsNullOrEmpty(GatewayCliTestData.NonShippingPackagesDir))
        {
            arguments.Append(" --add-source \"").Append(GatewayCliTestData.NonShippingPackagesDir.TrimEnd('\\', '/')).Append('"');
        }

        var psi = CreateStartInfo(GatewayCliTestData.DotNetHost, arguments.ToString(), installation._root);

        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (installation._installLog) { installation._installLog.AppendLine(e.Data); } } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (installation._installLog) { installation._installLog.AppendLine(e.Data); } } };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(milliseconds: 2 * 60 * 1000))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"'dotnet tool install' timed out.\n{installation._installLog}");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            installation.Dispose();
            throw new InvalidOperationException($"'dotnet tool install' failed with exit code {process.ExitCode}.\n{installation._installLog}");
        }

        return installation;
    }

    /// <summary>
    /// Launches the installed <c>blazor-gateway</c> command with the supplied arguments and waits for
    /// it to report the address it is listening on.
    /// </summary>
    public async Task<RunningGatewayTool> StartAsync(params string[] arguments)
    {
        var argLine = new StringBuilder("--urls http://127.0.0.1:0");
        foreach (var argument in arguments)
        {
            argLine.Append(' ').Append(argument);
        }

        var psi = CreateStartInfo(CommandPath, argLine.ToString(), _root);

        var log = new StringBuilder();
        var listeningUrl = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var process = new Process { StartInfo = psi };
        process.OutputDataReceived += OnData;
        process.ErrorDataReceived += OnData;

        void OnData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            lock (log)
            {
                log.AppendLine(e.Data);
            }

            var match = ListeningOnRegex().Match(e.Data);
            if (match.Success)
            {
                listeningUrl.TrySetResult(match.Groups[1].Value);
            }
        }

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completed = await Task.WhenAny(listeningUrl.Task, Task.Delay(TimeSpan.FromSeconds(60)));
        if (completed != listeningUrl.Task)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            process.Dispose();
            throw new TimeoutException($"The gateway tool did not start listening within the timeout.\n{log}");
        }

        return new RunningGatewayTool(process, await listeningUrl.Task, log);
    }

    private static ProcessStartInfo CreateStartInfo(string fileName, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // The gateway is framework-dependent: point the host at the repo's shared framework so it can
        // resolve Microsoft.AspNetCore.App, and keep the process from reaching outside the .dotnet folder.
        psi.Environment["DOTNET_ROOT"] = GatewayCliTestData.DotNetRoot;
        psi.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment.Remove("MSBuildSDKsPath");

        return psi;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    [GeneratedRegex(@"Now listening on:\s*(http://\S+)")]
    private static partial Regex ListeningOnRegex();
}

/// <summary>
/// A running <c>blazor-gateway</c> process started from an installed tool, exposing an
/// <see cref="HttpClient"/> bound to the address it is listening on.
/// </summary>
internal sealed class RunningGatewayTool : IAsyncDisposable
{
    private readonly Process _process;
    private readonly StringBuilder _log;

    public RunningGatewayTool(Process process, string listeningUrl, StringBuilder log)
    {
        _process = process;
        _log = log;
        ListeningUrl = listeningUrl;
        Client = new HttpClient { BaseAddress = new Uri(listeningUrl) };
    }

    public string ListeningUrl { get; }

    public HttpClient Client { get; }

    public string Output
    {
        get
        {
            lock (_log)
            {
                return _log.ToString();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();

        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The process may have already exited.
        }

        try
        {
            await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
        }
        catch
        {
            // Best effort.
        }

        _process.Dispose();
    }
}
