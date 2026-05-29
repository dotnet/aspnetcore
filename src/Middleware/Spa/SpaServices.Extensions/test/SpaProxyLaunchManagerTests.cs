// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.SpaProxy;
using Xunit;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Tests;

[Collection(SpaProxyLaunchManagerTestCollection.Name)]
public class SpaProxyLaunchManagerTests
{
    [Fact]
    public void ResolveLaunchCommand_UsesPnpmHomeWhenPnpmIsNotOnPath()
    {
        var root = Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));
        var pnpmHome = Path.Combine(root, "pnpm-home");
        var expectedCommand = CreateStandalonePnpmCommand(pnpmHome);

        WithEnvironmentVariables(
            path: Path.Combine(root, "path"),
            pnpmHome: pnpmHome,
            localAppData: Path.Combine(root, "localappdata"),
            home: Path.Combine(root, "home"),
            testCode: () => Assert.Equal(expectedCommand, SpaProxyLaunchManager.ResolveLaunchCommand("pnpm")));
    }

    [Fact]
    public void ResolveLaunchCommand_UsesDefaultStandaloneLocationWhenPnpmHomeIsNotSet()
    {
        var root = Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));
        var expectedCommand = CreateStandalonePnpmCommand(GetDefaultStandaloneDirectory(root));

        WithEnvironmentVariables(
            path: Path.Combine(root, "path"),
            pnpmHome: null,
            localAppData: Path.Combine(root, "localappdata"),
            home: Path.Combine(root, "home"),
            testCode: () => Assert.Equal(expectedCommand, SpaProxyLaunchManager.ResolveLaunchCommand("pnpm")));
    }

    [Fact]
    public void ResolveLaunchCommand_PrefersPathWhenPnpmCommandIsAvailable()
    {
        var root = Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));
        var pathDirectory = Path.Combine(root, "path");
        Directory.CreateDirectory(pathDirectory);
        File.WriteAllText(Path.Combine(pathDirectory, OperatingSystem.IsWindows() ? "pnpm.cmd" : "pnpm"), string.Empty);

        var pnpmHome = Path.Combine(root, "pnpm-home");
        CreateStandalonePnpmCommand(pnpmHome);

        WithEnvironmentVariables(
            path: pathDirectory,
            pnpmHome: pnpmHome,
            localAppData: Path.Combine(root, "localappdata"),
            home: Path.Combine(root, "home"),
            testCode: () => Assert.Equal(OperatingSystem.IsWindows() ? "pnpm.cmd" : "pnpm", SpaProxyLaunchManager.ResolveLaunchCommand("pnpm")));
    }

    private static string CreateStandalonePnpmCommand(string directory)
    {
        Directory.CreateDirectory(directory);

        var commandPath = Path.Combine(directory, OperatingSystem.IsWindows() ? "pnpm.exe" : "pnpm");
        File.WriteAllText(commandPath, string.Empty);
        return commandPath;
    }

    private static string GetDefaultStandaloneDirectory(string root)
        => OperatingSystem.IsWindows()
            ? Path.Combine(root, "localappdata", "pnpm")
            : Path.Combine(root, "home", ".local", "share", "pnpm");

    private static void WithEnvironmentVariables(string path, string? pnpmHome, string localAppData, string home, Action testCode)
    {
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        var originalPnpmHome = Environment.GetEnvironmentVariable("PNPM_HOME");
        var originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        var originalHome = Environment.GetEnvironmentVariable("HOME");

        try
        {
            Environment.SetEnvironmentVariable("PATH", path);
            Environment.SetEnvironmentVariable("PNPM_HOME", pnpmHome);
            Environment.SetEnvironmentVariable("LOCALAPPDATA", localAppData);
            Environment.SetEnvironmentVariable("HOME", home);

            testCode();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Environment.SetEnvironmentVariable("PNPM_HOME", originalPnpmHome);
            Environment.SetEnvironmentVariable("LOCALAPPDATA", originalLocalAppData);
            Environment.SetEnvironmentVariable("HOME", originalHome);

            var root = Directory.GetParent(path)?.FullName;
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public class SpaProxyLaunchManagerTestCollection
{
    public const string Name = nameof(SpaProxyLaunchManagerTestCollection);
}
