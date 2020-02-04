// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
            "Microsoft.DotNet.Web.ProjectTemplates.3.1",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.2.1",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.2.2",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.3.0",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates.3.1",
            "Microsoft.DotNet.Web.Spa.ProjectTemplates",
            "Microsoft.AspNetCore.Blazor.Templates",
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

            Assert.Single(builtPackages);

            /*
             * The templates are indexed by path, for example:
              &USERPROFILE%\.templateengine\dotnetcli\v3.0.100-preview7-012821\packages\nunit3.dotnetnew.template.1.6.1.nupkg
                Templates:
                    NUnit 3 Test Project (nunit) C#
                    NUnit 3 Test Item (nunit-test) C#
                    NUnit 3 Test Project (nunit) F#
                    NUnit 3 Test Item (nunit-test) F#
                    NUnit 3 Test Project (nunit) VB
                    NUnit 3 Test Item (nunit-test) VB
                Uninstall Command:
                    dotnet new -u &USERPROFILE%\.templateengine\dotnetcli\v3.0.100-preview7-012821\packages\nunit3.dotnetnew.template.1.6.1.nupkg

             * We don't want to construct this path so we'll rely on dotnet new --uninstall --help to construct the uninstall command.
             */
            var proc = await RunDotNetNew(output, "--uninstall --help");
            var lines = proc.Output.Split(Environment.NewLine);

            // Remove any previous or prebundled version of the template packages
            foreach (var packageName in _templatePackages)
            {
                // Depending on the ordering, there may be multiple matches:
                // Microsoft.DotNet.Web.Spa.ProjectTemplates.3.0.3.0.0-preview7.*.nupkg
                // Microsoft.DotNet.Web.Spa.ProjectTemplates.3.0.0-preview7.*.nupkg
                // Error on the side of caution and uninstall all of them
                foreach (var command in lines.Where(l => l.Contains("dotnet new") && l.Contains(packageName, StringComparison.OrdinalIgnoreCase)))
                {
                    var uninstallCommand = command.TrimStart();
                    Debug.Assert(uninstallCommand.StartsWith("dotnet new"));
                    uninstallCommand = uninstallCommand.Substring("dotnet new".Length);
                    await RunDotNetNew(output, uninstallCommand);
                }
            }

            await VerifyCannotFindTemplateAsync(output, "blazorwasm");

            foreach (var packagePath in builtPackages)
            {
                output.WriteLine($"Installing templates package {packagePath}...");
                var result = await RunDotNetNew(output, $"--install \"{packagePath}\"");
                Assert.True(result.ExitCode == 0, result.GetFormattedOutput());
            }

            await VerifyCanFindTemplate(output, "blazorwasm");
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

                if (!proc.Output.Contains("Couldn't find an installed template that matches the input, searching online for one that does..."))
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
