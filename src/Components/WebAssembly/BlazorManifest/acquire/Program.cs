using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace acquire
{
    class Program
    {
        const string ManifestVersion = "6.0.100";

        static string MuxerPath { get; } = GetDotnetPath();

        static string GetDotnetPath()
        {
            // Process.MainModule is app[.exe] and not `dotnet`. We can instead calculate the dotnet SDK path
            // by looking at the shared fx directory instead.
            // depsFile = /dotnet/shared/Microsoft.NETCore.App/6.0-preview2/Microsoft.NETCore.App.deps.json
            var depsFile = (string)AppContext.GetData("FX_DEPS_FILE");
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(depsFile), "..", "..", "..", "dotnet" + (OperatingSystem.IsWindows() ? ".exe" : "")));
        }

        static int Main(string[] args)
        {
            var sdkDirectory = args.Length > 0 ? args[0] : Path.GetDirectoryName(MuxerPath);
            var tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tmp", Path.GetRandomFileName());
            var restoreDirectory = Path.Combine(tempDirectory, ".nuget");

            try
            {
                var packs = GetPacks(sdkDirectory);
                var restore = RestorePacks(tempDirectory, restoreDirectory, packs);
                if (restore != 0)
                {
                    return restore;
                }

                foreach (var (id, version) in packs)
                {
                    var source = Path.Combine(restoreDirectory, id.ToLowerInvariant(), version);
                    var destination = Path.Combine(sdkDirectory, "packs", id, version);

                    Move(source, destination);
                }

                var sdkVersionProc = Process.Start(new ProcessStartInfo
                {
                    FileName = MuxerPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                });
                sdkVersionProc.WaitForExit();
                var sdkVersion = sdkVersionProc.StandardOutput.ReadToEnd().Trim();
                var sentinelPath = Path.Combine(sdkDirectory, "sdk", sdkVersion, "EnableWorkloadResolver.sentinel");
                Console.WriteLine($"Enabling Workloads support in dotnet SDK v{sdkVersion}.");

                File.WriteAllBytes(sentinelPath, Array.Empty<byte>());
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }

            return 0;
        }

        static void Move(string source, string destination)
        {
            Console.WriteLine($"Moving {source} to {destination}...");
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, recursive: true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination));

            Directory.Move(source, destination);
        }

        static List<(string Id, string Version)> GetPacks(string sdkDirectory)
        {
            var manifestDirectory = Path.Combine(sdkDirectory, "sdk-manifests", ManifestVersion, "Microsoft.NET.Workload.BlazorWebAssembly");
            if (!Directory.Exists(manifestDirectory))
            {
                throw new DirectoryNotFoundException($"Cound not find directory {manifestDirectory}. A 6.0-preview3 SDK or newer is required for this tool to function.");
            }

            var manifestPath = Path.Combine(manifestDirectory, "WorkloadManifest.json");
            var manifest = JsonSerializer.Deserialize<PackInformation>(File.ReadAllBytes(manifestPath), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var packs = new List<(string, string)>();
            foreach (var item in manifest.Packs)
            {
                var packageName = item.Key;
                if (item.Value.AliasTo is Dictionary<string, string> alias)
                {
                    packageName = ""; // if this is an alias pack and platform isn't in the alias it isn't required

                    if (OperatingSystem.IsWindows())
                    {
                        alias.TryGetValue(Environment.Is64BitProcess ? "win-x64" : "win-x86", out packageName);
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        alias.TryGetValue("osx-x64", out packageName);
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        alias.TryGetValue("linux-x64", out packageName);
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported OS platform.");
                    }
                }

                if (!string.IsNullOrEmpty(packageName))
                {
                    packs.Add((packageName, item.Value.Version));
                }
            }

            return packs;
        }

        static int RestorePacks(string tempDirectory, string restoreDirectory, List<(string Id, string Version)> packs)
        {
            var restoreProject = Path.Combine(tempDirectory, "restore", "Restore.csproj");
            var restoreProjectDirectory = Directory.CreateDirectory(Path.GetDirectoryName(restoreProject));

            File.WriteAllText(Path.Combine(restoreProjectDirectory.FullName, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(restoreProjectDirectory.FullName, "Directory.Build.targets"), "<Project />");

            var projectFile = @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <NoWarn>$(NoWarn);NU1213</NoWarn>
    </PropertyGroup>
    <ItemGroup>
";
            foreach (var (Id, Version) in packs)
            {
                projectFile += $"<PackageReference Include=\"{Id}\" Version=\"{Version}\" />";
            }

            projectFile += @"
    </ItemGroup>
</Project>
";
            File.WriteAllText(restoreProject, projectFile);

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = MuxerPath,
                ArgumentList = { "restore", restoreProject },
#if !DEBUG
                RedirectStandardError = true,
                RedirectStandardOutput = true,
#endif
                Environment =
                {
                    ["NUGET_PACKAGES"] = restoreDirectory,
                },
            });
            process.WaitForExit();
            return 0;
        }

        private record PackInformation(IDictionary<string, PackVersionInformation> Packs);

        private record PackVersionInformation(string Version, [property: JsonPropertyName("alias-to")] Dictionary<string, string> AliasTo);
    }
}
