// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    internal static class TemplatePackageInstaller
    {
        private static SemaphoreSlim InstallerLock = new SemaphoreSlim(1);
        private static bool _haveReinstalledTemplatePackages;

        private static readonly string[] _templatePackages = new[]
        {
            "Microsoft.DotNet.Common.ItemTemplates",
            "Microsoft.DotNet.Common.ProjectTemplates.2.1",
            "Microsoft.DotNet.Test.ProjectTemplates.2.1",
            "Microsoft.DotNet.Web.Client.ItemTemplates",
            "Microsoft.DotNet.Web.ItemTemplates",
            "Microsoft.DotNet.Web.ProjectTemplates.1.x",
            "Microsoft.DotNet.Web.ProjectTemplates.2.0",
            "Microsoft.DotNet.Web.ProjectTemplates.2.1",
            "Microsoft.DotNet.Web.ProjectTemplates.2.2",
            "Microsoft.DotNet.Web.ProjectTemplates.3.0",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.2.2",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.3.0"
        };

        public static string CustomHivePath { get; } = typeof(TemplatePackageInstaller)
            .Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(s => s.Key == "CustomTemplateHivePath").Value;

        public static async Task EnsureTemplatingEngineInitializedAsync(ITestOutputHelper output)
        {
            await InstallerLock.WaitAsync();
            try
            {
                if (!_haveReinstalledTemplatePackages)
                {
                    if (Directory.Exists(CustomHivePath))
                    {
                        Directory.Delete(CustomHivePath, recursive: true);
                    }
                    await InstallTemplatePackages(output);
                    _haveReinstalledTemplatePackages = true;
                }
            }
            finally
            {
                InstallerLock.Release();
            }
        }

        public static async Task<ProcessEx> RunDotNetNew(ITestOutputHelper output, string arguments)
        {
            var proc = ProcessEx.Run(
            output,
            AppContext.BaseDirectory,
            DotNetMuxer.MuxerPathOrDefault(),
            $"new {arguments} --debug:custom-hive \"{CustomHivePath}\"");
            await proc.Exited;

            return proc;
        }

        private static async Task InstallTemplatePackages(ITestOutputHelper output)
        {
            var builtPackages = Directory.EnumerateFiles(
                    typeof(TemplatePackageInstaller).Assembly
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .Single(a => a.Key == "ArtifactsShippingPackagesDir").Value,
                    "*.nupkg")
                .Where(p => _templatePackages.Any(t => Path.GetFileName(p).StartsWith(t, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            Assert.Equal(4, builtPackages.Length);

            // Remove any previous or prebundled version of the template packages
            foreach (var packageName in _templatePackages)
            {
                // We don't need this command to succeed, because we'll verify next that
                // uninstallation had the desired effect. This command is expected to fail
                // in the case where the package wasn't previously installed.
                await RunDotNetNew(output, $"--uninstall {packageName}");
            }

            await VerifyCannotFindTemplateAsync(output, "web");
            await VerifyCannotFindTemplateAsync(output, "webapp");
            await VerifyCannotFindTemplateAsync(output, "mvc");
            await VerifyCannotFindTemplateAsync(output, "react");
            await VerifyCannotFindTemplateAsync(output, "reactredux");
            await VerifyCannotFindTemplateAsync(output, "angular");

            foreach (var packagePath in builtPackages)
            {
                output.WriteLine($"Installing templates package {packagePath}...");
                var result = await RunDotNetNew(output, $"--install \"{packagePath}\"");
                Assert.True(result.ExitCode == 0, result.GetFormattedOutput());
            }

            await VerifyCanFindTemplate(output, "webapp");
            await VerifyCanFindTemplate(output, "web");
            await VerifyCanFindTemplate(output, "react");
        }

        private static async Task VerifyCanFindTemplate(ITestOutputHelper output, string templateName)
        {
            var proc = await RunDotNetNew(output, $"");
            if (!proc.Output.Contains($" {templateName} "))
            {
                throw new InvalidOperationException($"Couldn't find {templateName} as an option in {proc.Output}.");
            }
        }

        private static async Task VerifyCannotFindTemplateAsync(ITestOutputHelper output, string templateName)
        {
            // Verify we really did remove the previous templates
            var tempDir = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName(), Guid.NewGuid().ToString("D"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var proc = await RunDotNetNew(output, $"\"{templateName}\"");

                if (!proc.Error.Contains($"No templates matched the input template name: {templateName}."))
                {
                    throw new InvalidOperationException($"Failed to uninstall previous templates. The template '{templateName}' could still be found.");
                }
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
