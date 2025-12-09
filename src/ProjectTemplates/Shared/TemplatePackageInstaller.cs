// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

namespace Templates.Test.Helpers;

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
            "Microsoft.DotNet.Web.ProjectTemplates.7.0",
            "Microsoft.DotNet.Web.ProjectTemplates.8.0",
            "Microsoft.DotNet.Web.ProjectTemplates.9.0",
            "Microsoft.DotNet.Web.ProjectTemplates.10.0",
            "Microsoft.AspNetCore.Blazor.Templates",
        };

    public static string CustomHivePath { get; } = Path.GetFullPath((string.IsNullOrEmpty(Environment.GetEnvironmentVariable("helix")))
                 ? typeof(TemplatePackageInstaller)
                     .Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                     .Single(s => s.Key == "CustomTemplateHivePath").Value
                 : Path.Combine("Hives", ".templateEngine"));

    public static async Task EnsureTemplatingEngineInitializedAsync(ITestOutputHelper output)
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

    public static async Task<ProcessEx> RunDotNetNew(ITestOutputHelper output, string arguments)
    {
        var proc = ProcessEx.Run(
            output,
            AppContext.BaseDirectory,
            DotNetMuxer.MuxerPathOrDefault(),
            //--debug:disable-sdk-templates means, don't include C:\Program Files\dotnet\templates, aka. what comes with SDK, so we don't need to uninstall
            //--debug:custom-hive means, don't install templates on CI/developer machine, instead create new temporary instance
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

        if (builtPackages.Length == 0)
        {
            throw new InvalidOperationException($"Failed to find required templates in {packagesDir}. Please ensure the *Templates*.nupkg have been built.");
        }

        Assert.Equal(3, builtPackages.Length);

        await VerifyCannotFindTemplateAsync(output, "web");
        await VerifyCannotFindTemplateAsync(output, "webapp");
        await VerifyCannotFindTemplateAsync(output, "webapi");
        await VerifyCannotFindTemplateAsync(output, "mvc");

        foreach (var packagePath in builtPackages)
        {
            output.WriteLine($"Installing templates package {packagePath}...");
            var result = await RunDotNetNew(output, $"install \"{packagePath}\"");
            Assert.True(result.ExitCode == 0, result.GetFormattedOutput());
        }

        await VerifyCanFindTemplate(output, "webapp");
        await VerifyCanFindTemplate(output, "web");
        await VerifyCanFindTemplate(output, "webapi");
    }

    private static async Task VerifyCanFindTemplate(ITestOutputHelper output, string templateName)
    {
        var proc = await RunDotNetNew(output, $"--list");
        if (!(proc.Output.Contains($" {templateName} ") || proc.Output.Contains($",{templateName}") || proc.Output.Contains($"{templateName},")))
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

            if (!proc.Error.Contains("No templates or subcommands found matching:"))
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
