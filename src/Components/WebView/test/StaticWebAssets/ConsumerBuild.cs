// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.Components.WebView.StaticWebAssets;

/// <summary>
/// Creates a throwaway solution on disk that references the locally-built WebView package and runs
/// the repo SDK (.dotnet) to build/publish it. Used to validate that consuming the package produces
/// the expected static web asset endpoints (issue #67374).
/// </summary>
internal sealed class ConsumerBuild : IDisposable
{
    private readonly string _root;
    private readonly string _packagesFolder;

    public ConsumerBuild()
    {
        _root = Path.Combine(Path.GetTempPath(), "wv-swa-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _packagesFolder = Path.Combine(_root, ".nuget-packages");

        // Isolate the build from the repo and from any other test run.
        File.WriteAllText(Path.Combine(_root, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_root, "Directory.Build.targets"), "<Project />");

        // Use an isolated global-packages folder so the freshly-built package under test is never
        // served stale from a shared cache, while adding the repo's package cache as a read-only
        // fallback so the exact transitive package versions the repo restored (which may not be
        // published to public feeds yet) can still be resolved.
        var repoCache = StaticWebAssetsTestData.NuGetPackageRoot.TrimEnd('\\', '/');
        File.WriteAllText(Path.Combine(_root, "NuGet.config"), $"""
            <configuration>
              <config>
                <add key="globalPackagesFolder" value="{_packagesFolder}" />
              </config>
              <fallbackPackageFolders>
                <clear />
                <add key="repo-cache" value="{repoCache}" />
              </fallbackPackageFolders>
              <packageSources>
                <clear />
                <add key="local-shipping" value="{StaticWebAssetsTestData.ShippingPackagesDir.TrimEnd('\\', '/')}" />
                <add key="local-nonshipping" value="{StaticWebAssetsTestData.NonShippingPackagesDir.TrimEnd('\\', '/')}" />
                <add key="dotnet-public" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" />
                <add key="nuget" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);
    }

    public string Root => _root;

    public string CreateProject(string relativeDir, string fileName, string content)
    {
        var dir = Path.Combine(_root, relativeDir);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    public void CreateFile(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    public ProcessResult Run(string arguments, string projectRelativePath)
    {
        // The package version under test is constant (e.g. 11.0.0-dev). Make sure a previously
        // extracted copy in the shared repo cache (used as a fallback folder) can't shadow the
        // freshly built package; restore will then pull it from the local feed.
        EvictFromFallbackCache("Microsoft.AspNetCore.Components.WebView");

        var psi = new ProcessStartInfo(StaticWebAssetsTestData.DotNetHost)
        {
            Arguments = $"{arguments} \"{Path.Combine(_root, projectRelativePath)}\"",
            WorkingDirectory = _root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // Keep the build hermetic: pin the repo runtime and stop the SDK from reaching outside the
        // .dotnet folder. The global-packages folder is configured via NuGet.config.
        psi.Environment["DOTNET_ROOT"] = Path.Combine(StaticWebAssetsTestData.RepoRoot, ".dotnet");
        psi.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment.Remove("MSBuildSDKsPath");

        var output = new StringBuilder();
        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (output) { output.AppendLine(e.Data); } } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (output) { output.AppendLine(e.Data); } } };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(milliseconds: 5 * 60 * 1000))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"'dotnet {arguments}' timed out.\n{output}");
        }

        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output.ToString());
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

    private static void EvictFromFallbackCache(string packageId)
    {
        var dir = Path.Combine(
            StaticWebAssetsTestData.NuGetPackageRoot,
            packageId.ToLowerInvariant(),
            StaticWebAssetsTestData.PackageVersion);

        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // Best effort; if it can't be removed restore may still succeed from the local feed.
        }
    }
}

internal sealed record ProcessResult(int ExitCode, string Output)
{
    public bool Succeeded => ExitCode == 0;

    /// <summary>
    /// True when the failure looks like it was caused by an inability to reach the NuGet feeds rather
    /// than a real build problem, so offline environments can skip instead of failing.
    /// </summary>
    public bool LooksLikeNetworkFailure
        => !Succeeded &&
           (Output.Contains("Unable to load the service index", StringComparison.OrdinalIgnoreCase) ||
            Output.Contains("NU1301", StringComparison.OrdinalIgnoreCase) ||
            Output.Contains("Unable to resolve", StringComparison.OrdinalIgnoreCase) && Output.Contains("nuget", StringComparison.OrdinalIgnoreCase) ||
            Output.Contains("The remote name could not be resolved", StringComparison.OrdinalIgnoreCase) ||
            Output.Contains("No such host is known", StringComparison.OrdinalIgnoreCase));
}
