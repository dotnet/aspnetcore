// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    internal static class TemplatePackageInstaller
    {
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
            "Microsoft.DotNet.Web.ProjectTemplates.3.1",
            "Microsoft.DotNet.Web.ProjectTemplates.5.0",
            "Microsoft.DotNet.Web.ProjectTemplates.6.0",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.2.1",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.2.2",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.3.0",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.3.1",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.5.0",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.6.0",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates",
            "Microsoft.AspNetCore.Blazor.Templates",
        };

        public static string CustomHivePath { get; } = (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("helix")))
                     ? typeof(TemplatePackageInstaller)
                         .Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                         .Single(s => s.Key == "CustomTemplateHivePath").Value
                     : Path.Combine("Hives", ".templateEngine");

        public static async Task EnsureTemplatingEngineInitializedAsync(ITestOutputHelper output)
        {
            await ProcessLock.DotNetNewLock.WaitAsync();
            try
            {
                output.WriteLine("Acquired DotNetNewLock");
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
                ProcessLock.DotNetNewLock.Release();
                output.WriteLine("Released DotNetNewLock");
            }
        }

        public static async Task<ProcessEx> RunDotNetNew(ITestOutputHelper output, string arguments)
        {
            var proc = ProcessEx.Run(
                output,
                AppContext.BaseDirectory,
                DotNetMuxer.MuxerPathOrDefault(),
                $"new {arguments} --debug:disable-sdk-templates --debug:custom-hive \"{CustomHivePath}\"");

            await proc.Exited;

            return proc;
        }

        private static async Task InstallTemplatePackages(ITestOutputHelper output)
        {
            string packagesDir;
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("helix")))
            {
                packagesDir = ".";
            }
            else
            {
                packagesDir = typeof(TemplatePackageInstaller).Assembly
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .Single(a => a.Key == "ArtifactsShippingPackagesDir").Value;
            }

            var builtPackages = Directory.EnumerateFiles(packagesDir, "*Templates*.nupkg")
                .Where(p => _templatePackages.Any(t => Path.GetFileName(p).StartsWith(t, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            Assert.Equal(4, builtPackages.Length);

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

                if (!proc.Error.Contains("No templates found matching:"))
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
