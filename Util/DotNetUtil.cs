// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json;
using NuGet.Versioning;

namespace Cli.FunctionalTests.Util
{
    internal static class DotNetUtil
    {
        private const string _clearPackageSourcesNuGetConfig =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
";

        // Bind to dynamic port 0 to avoid port conflicts during parallel tests
        private const string _urls = "--urls http://127.0.0.1:0;https://127.0.0.1:0";

        // Must publish to folder under "bin" or "obj" to prevent double-copying publish output during incremental publish
        public static string PublishOutput => Path.Combine("bin", "pub");

        private static readonly Lazy<(SemanticVersion SdkVersion, SemanticVersion RuntimeVersion)> _versions =
            new Lazy<(SemanticVersion SdkVersion, SemanticVersion RuntimeVersion)>(GetVersions, LazyThreadSafetyMode.PublicationOnly);

        public static SemanticVersion SdkVersion => _versions.Value.SdkVersion;

        public static SemanticVersion RuntimeVersion => _versions.Value.RuntimeVersion;

        private static readonly Lazy<bool> _requiresPrivateFeed = new Lazy<bool>(GetRequiresPrivateFeed, LazyThreadSafetyMode.PublicationOnly);

        public static bool RequiresPrivateFeed => _requiresPrivateFeed.Value;

        public static string TargetFrameworkMoniker => $"netcoreapp{RuntimeVersion.Major}.{RuntimeVersion.Minor}";

        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly IEnumerable<KeyValuePair<string, string>> _globalEnvironment = new KeyValuePair<string, string>[] {
            // Ignore globally-installed .NET Core components
            new KeyValuePair<string, string>("DOTNET_MULTILEVEL_LOOKUP", "false"),
        };

        private static (SemanticVersion SdkVersion, SemanticVersion RuntimeVersion) GetVersions()
        {
            var info = RunDotNet("--info", workingDirectory: null);

            var sdkVersionString = Regex.Match(info, @"Version:\s*(\S+)").Groups[1].Value;
            var sdkVersion = SemanticVersion.Parse(sdkVersionString);

            // Select highest version of Microsoft.NETCore.App which matches major and minor version of SDK
            var runtimeVersionPattern = $@"Microsoft.NETCore.App\s*({sdkVersion.Major}.{sdkVersion.Minor}\S+)";
            var runtimeVersionString = Regex.Match(info, runtimeVersionPattern, RegexOptions.RightToLeft).Groups[1].Value;
            var runtimeVersion = SemanticVersion.Parse(runtimeVersionString);

            // Supported version range is [2.1.300,2.2.100] (inclusive)
            if (sdkVersion >= new SemanticVersion(2, 1, 300) && sdkVersion <= new SemanticVersion(2, 2, 100))
            {
                return (sdkVersion, runtimeVersion);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported SDK version: {sdkVersion}");
            }
        }

        // Private feed is required if nuget.org doesn't contain the matching version of Microsoft.NETCore.App
        private static bool GetRequiresPrivateFeed()
        {
            var versionString = _httpClient.GetStringAsync("https://api.nuget.org/v3-flatcontainer/microsoft.netcore.app/index.json").Result;
            var definition = new { Versions = Enumerable.Empty<string>() };

            var versions = JsonConvert.DeserializeAnonymousType(versionString, definition);
            return !versions.Versions.Contains(RuntimeVersion.ToString());
        }

        private static IEnumerable<KeyValuePair<string, string>> GetEnvironment(NuGetPackageSource nuGetPackageSource)
        {
            // Set NUGET_PACKAGES to an initially-empty, distinct folder for each NuGetPackageSource.  This ensures packages are loaded
            // from either NuGetFallbackFolder or configured sources, and *not* loaded from the default per-user global-packages folder.
            // 
            // [5/7/2018] NUGET_PACKAGES cannot be set to a folder under the application due to https://github.com/dotnet/cli/issues/9216.
            yield return new KeyValuePair<string, string>("NUGET_PACKAGES", Path.Combine(AssemblySetUp.TempDir, nuGetPackageSource.Name));
        }

        public static string New(string template, string workingDirectory)
        {
            // Clear all packages sources by default.  May be overridden by NuGetPackageSource parameter.
            File.WriteAllText(Path.Combine(workingDirectory, "NuGet.config"), _clearPackageSourcesNuGetConfig);

            // Pass "--debug:ephemeral-hive" to build template contents in-memory, rather than using the default
            // "%UserProfile%\.templateengine" cache, which may be out-of-date when testing newer builds with the same version.
            return RunDotNet($"new {template} --name {template} --output . --no-restore --debug:ephemeral-hive", workingDirectory);
        }

        public static string Restore(string workingDirectory, NuGetPackageSource packageSource, RuntimeIdentifier runtimeIdentifier)
        {
            return RunDotNet($"restore /warnaserror --no-cache {packageSource.SourceArgument} {runtimeIdentifier.RuntimeArgument}",
                workingDirectory, GetEnvironment(packageSource));
        }

        public static string Build(string workingDirectory, NuGetPackageSource packageSource, RuntimeIdentifier runtimeIdentifier)
        {
            // "dotnet build" cannot use "--no-restore" if the app is self-contained and the SDK contains a patched runtime
            // https://github.com/dotnet/sdk/issues/2312, https://github.com/dotnet/cli/issues/9514
            bool restoreRequired = (runtimeIdentifier != RuntimeIdentifier.None) && (DotNetUtil.RuntimeVersion.Patch > 0);

            var restoreArgument = restoreRequired ? $"--no-cache {packageSource.SourceArgument}" : "--no-restore";

            return RunDotNet($"build /warnaserror {restoreArgument} {runtimeIdentifier.RuntimeArgument}", workingDirectory, GetEnvironment(packageSource));
        }

        public static (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) Run(
            string workingDirectory, RuntimeIdentifier runtimeIdentifier)
        {
            return StartDotNet($"run --no-build {_urls} {runtimeIdentifier.RuntimeArgument}", workingDirectory);
        }

        public static string Publish(string workingDirectory, RuntimeIdentifier runtimeIdentifier)
        {
            return RunDotNet($"publish --no-build -o {PublishOutput} {runtimeIdentifier.RuntimeArgument}", workingDirectory);
        }

        internal static (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) Exec(
            string workingDirectory, string name, RuntimeIdentifier runtimeIdentifier)
        {
            if (runtimeIdentifier == RuntimeIdentifier.None)
            {
                var path = Path.Combine(PublishOutput, $"{name}.dll");
                return StartDotNet($"exec {path} {_urls}", workingDirectory);
            }
            else
            {
                var file = (runtimeIdentifier == RuntimeIdentifier.Win_x64) ? $"{name}.exe" : name;
                var path = Path.Combine(workingDirectory, PublishOutput, file);
                return StartProcess(path, _urls, workingDirectory);
            }
        }

        private static string RunDotNet(string arguments, string workingDirectory,
            IEnumerable<KeyValuePair<string, string>> environment = null, bool throwOnError = true)
        {
            var p = StartDotNet(arguments, workingDirectory, environment);
            return WaitForExit(p, throwOnError: throwOnError);
        }

        private static (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) StartDotNet(
            string arguments, string workingDirectory, IEnumerable<KeyValuePair<string, string>> environment = null)
        {
            var env = _globalEnvironment.Concat(environment ?? Enumerable.Empty<KeyValuePair<string, string>>());
            return StartProcess("dotnet", arguments, workingDirectory, env);
        }

        private static (Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) StartProcess(
            string filename, string arguments, string workingDirectory, IEnumerable<KeyValuePair<string, string>> environment = null)
        {
            var process = new Process()
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                },
            };

            if (environment != null)
            {
                foreach (var kvp in environment)
                {
                    process.StartInfo.Environment.Add(kvp);
                }
            }

            var outputBuilder = new ConcurrentStringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                outputBuilder.AppendLine(e.Data);
            };

            var errorBuilder = new ConcurrentStringBuilder();
            process.ErrorDataReceived += (_, e) =>
            {
                errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return (process, outputBuilder, errorBuilder);
        }

        public static string StopProcess((Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) process,
            bool throwOnError = true)
        {
            if (!process.Process.HasExited)
            {
                process.Process.KillTree();
            }

            return WaitForExit(process, throwOnError: throwOnError);
        }

        public static string WaitForExit((Process Process, ConcurrentStringBuilder OutputBuilder, ConcurrentStringBuilder ErrorBuilder) process,
            bool throwOnError = true)
        {
            // Workaround issue where WaitForExit() blocks until child processes are killed, which is problematic
            // for the dotnet.exe NodeReuse child processes.  I'm not sure why this is problematic for dotnet.exe child processes
            // but not for MSBuild.exe child processes.  The workaround is to specify a large timeout.
            // https://stackoverflow.com/a/37983587/102052
            process.Process.WaitForExit(int.MaxValue);

            if (throwOnError && process.Process.ExitCode != 0)
            {
                var sb = new ConcurrentStringBuilder();

                sb.AppendLine($"Command {process.Process.StartInfo.FileName} {process.Process.StartInfo.Arguments} returned exit code {process.Process.ExitCode}");
                sb.AppendLine();
                sb.AppendLine(process.OutputBuilder.ToString());

                throw new InvalidOperationException(sb.ToString());
            }

            return process.OutputBuilder.ToString();
        }
    }
}
