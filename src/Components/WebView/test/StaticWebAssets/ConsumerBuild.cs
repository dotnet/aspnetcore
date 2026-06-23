// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.WebView.StaticWebAssets;

/// <summary>
/// Creates a throwaway solution on disk that references the locally-built WebView package and runs
/// the repo SDK (.dotnet) to build/publish it. Used to validate that consuming the package produces
/// the expected static web asset endpoints (issue #67374).
///
/// Working folders live under the repo's artifacts/tmp directory (not the system temp folder), and
/// every build/publish captures a binary log under artifacts/log so failures can be diagnosed from
/// CI. The working folder is preserved when a build fails and removed on success.
/// </summary>
internal sealed class ConsumerBuild : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _root;
    private readonly string _packagesFolder;
    private readonly string _id;
    private bool _preserve;

    public ConsumerBuild(ITestOutputHelper output, bool isolateNuGetFeeds = true, [CallerMemberName] string testName = "")
    {
        _output = output;
        _id = $"{testName}-{Guid.NewGuid():N}";
        _root = Path.Combine(StaticWebAssetsTestData.ArtifactsTmpDir, "ComponentsWebViewStaticWebAssetsTests", _id);
        Directory.CreateDirectory(_root);
        _packagesFolder = Path.Combine(_root, ".nuget-packages");

        // Isolate the build from the repo and from any other test run.
        File.WriteAllText(Path.Combine(_root, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(_root, "Directory.Build.targets"), "<Project />");

        if (!isolateNuGetFeeds)
        {
            // ProjectReference (P2P) mode: the app references the WebView source project, so it needs
            // no package feed of its own. Inherit the repo's NuGet.config (the working folder lives
            // under the repo's artifacts) so the referenced project's dependencies resolve.
            return;
        }

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

    /// <param name="verb">The dotnet verb plus its options, e.g. "publish -c Release".</param>
    /// <param name="projectRelativePath">Project to build, relative to the working folder.</param>
    public ProcessResult Run(string verb, string projectRelativePath)
    {
        // The package version under test is constant (e.g. 11.0.0-dev). Make sure a previously
        // extracted copy in the shared repo cache (used as a fallback folder) can't shadow the
        // freshly built package; restore will then pull it from the local feed.
        EvictFromFallbackCache("Microsoft.AspNetCore.Components.WebView");

        // Capture a binary log under artifacts/log so CI uploads it and failures can be analyzed.
        var verbName = verb.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "build";
        var binlogPath = Path.Combine(StaticWebAssetsTestData.ArtifactsLogDir, $"WebViewStaticWebAssets-{_id}-{verbName}.binlog");
        Directory.CreateDirectory(StaticWebAssetsTestData.ArtifactsLogDir);

        var arguments = $"{verb} \"{Path.Combine(_root, projectRelativePath)}\" -bl:\"{binlogPath}\"";
        var psi = new ProcessStartInfo(StaticWebAssetsTestData.DotNetHost)
        {
            Arguments = arguments,
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

        _output.WriteLine($"> dotnet {arguments}");

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
            _preserve = true;
            throw new TimeoutException($"'dotnet {verb}' timed out. Binlog: {binlogPath}\n{output}");
        }

        process.WaitForExit();
        var result = new ProcessResult(process.ExitCode, output.ToString(), binlogPath);

        _output.WriteLine(result.Output);
        _output.WriteLine($"Exit code: {result.ExitCode}. Binlog: {binlogPath}");
        if (!result.Succeeded)
        {
            // Leave the working folder in place so the failure can be investigated locally.
            _preserve = true;
        }

        return result;
    }

    public void Dispose()
    {
        if (_preserve)
        {
            _output.WriteLine($"Build failed; preserving working folder for investigation: {_root}");
            return;
        }

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

internal sealed record ProcessResult(int ExitCode, string Output, string BinlogPath)
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
