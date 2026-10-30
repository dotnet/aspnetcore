// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Installs the built <c>Microsoft.AspNetCore.Components.Gateway.Cli</c> package as a dotnet tool and
/// asserts it is a well-formed RID-specific tool under the <c>blazor-gateway</c> command.
/// Installation tests use a throwaway tool-path to exercise the same acquisition path as a real
/// consumer; package-shape tests inspect the locally built archives.
/// </summary>
[RequiresBuiltGatewayCliPackage]
public class BlazorGatewayCliPackageTests
{
    private static string ToolDir => $"tools/{GatewayCliTestData.DefaultTargetFramework}/any";
    private static string AnyToolDir => $"tools/{GatewayCliTestData.DefaultTargetFramework}/any";
    private static string NativeToolDir => $"tools/any/{GatewayCliTestData.HostRuntimeIdentifier}";

    [ConditionalFact]
    public void Package_IsMarkedAsDotnetTool()
    {
        using var tool = GatewayToolInstallation.Install();

        var nuspec = XDocument.Parse(tool.ReadNuspec());
        var ns = nuspec.Root!.Name.Namespace;

        var packageTypes = nuspec.Root
            .Element(ns + "metadata")?
            .Element(ns + "packageTypes")?
            .Elements(ns + "packageType")
            .Select(e => e.Attribute("name")?.Value)
            .ToArray() ?? [];

        Assert.Contains("DotnetTool", packageTypes);
    }

    [ConditionalFact]
    public void Package_ContainsDotnetToolSettings_ForAllRuntimePackages()
    {
        using var tool = GatewayToolInstallation.Install();

        Assert.True(
            tool.HasFile($"{ToolDir}/DotnetToolSettings.xml"),
            $"Expected DotnetToolSettings.xml under {ToolDir}.");

        var settings = XDocument.Parse(tool.ReadFile($"{ToolDir}/DotnetToolSettings.xml"));
        var command = settings.Descendants("Command").Single();

        Assert.Equal(GatewayCliTestData.ToolCommandName, command.Attribute("Name")?.Value);

        var runtimePackages = settings.Descendants("RuntimeIdentifierPackage")
            .ToDictionary(
                element => element.Attribute("RuntimeIdentifier")!.Value,
                element => element.Attribute("Id")!.Value,
                StringComparer.Ordinal);

        string[] expectedRuntimeIdentifiers = GatewayCliTestData.IsNativePackageAvailable
            ? [.. GatewayCliTestData.NativeRuntimeIdentifiers, "any"]
            : ["any"];
        foreach (var runtimeIdentifier in expectedRuntimeIdentifiers)
        {
            Assert.Equal(
                $"{GatewayCliTestData.PackageId}.{runtimeIdentifier}",
                runtimePackages[runtimeIdentifier]);
        }

        Assert.Equal(expectedRuntimeIdentifiers.Length, runtimePackages.Count);
    }

    [ConditionalFact]
    public void AnyPackage_ContainsFrameworkDependentGateway()
    {
        Assert.True(
            PackageHasFile(GatewayCliTestData.AnyPackageId, $"{AnyToolDir}/blazor-gateway.dll"),
            "Missing framework-dependent blazor-gateway.dll.");
        Assert.True(
            PackageHasFile(GatewayCliTestData.AnyPackageId, $"{AnyToolDir}/blazor-gateway.deps.json"),
            "Missing framework-dependent blazor-gateway.deps.json.");
        Assert.True(
            PackageHasFile(GatewayCliTestData.AnyPackageId, $"{AnyToolDir}/blazor-gateway.runtimeconfig.json"),
            "Missing framework-dependent blazor-gateway.runtimeconfig.json.");
    }

    [ConditionalFact]
    public void AnyPackage_RuntimeConfigPreservesGatewayRollForwardPolicy()
    {
        var runtimeConfig = ReadPackageFile(
            GatewayCliTestData.AnyPackageId,
            $"{AnyToolDir}/blazor-gateway.runtimeconfig.json");

        using var document = JsonDocument.Parse(runtimeConfig);
        var runtimeOptions = document.RootElement.GetProperty("runtimeOptions");
        var framework = runtimeOptions.GetProperty("framework");

        Assert.Equal("Microsoft.AspNetCore.App", framework.GetProperty("name").GetString());
        Assert.Equal(GatewayCliTestData.FrameworkVersion, framework.GetProperty("version").GetString());
        Assert.Equal(2, runtimeOptions.GetProperty("rollForwardOnNoCandidateFx").GetInt32());
    }

    [ConditionalFact]
    public void HostPackage_UsesNativeExecutable_WhenSupported()
    {
        using var tool = GatewayToolInstallation.Install();

        if (!GatewayCliTestData.IsNativePackageAvailable)
        {
            Assert.Equal(GatewayCliTestData.AnyPackageId, tool.SelectedRuntimePackageId);
            return;
        }

        var executableName = OperatingSystem.IsWindows() ? "blazor-gateway.exe" : "blazor-gateway";
        Assert.Equal(GatewayCliTestData.HostRuntimePackageId, tool.SelectedRuntimePackageId);
        Assert.True(
            tool.SelectedRuntimePackageHasFile($"{NativeToolDir}/{executableName}"),
            $"Missing native executable for {GatewayCliTestData.HostRuntimeIdentifier}.");
        Assert.False(tool.SelectedRuntimePackageHasFile($"{NativeToolDir}/blazor-gateway.dll"));
        Assert.False(tool.SelectedRuntimePackageHasFile($"{NativeToolDir}/blazor-gateway.runtimeconfig.json"));
        Assert.False(tool.SelectedRuntimePackageHasFile($"{NativeToolDir}/blazor-gateway.pdb"));
    }

    [ConditionalFact]
    public void HostPackage_DoesNotContainDebugSymbols()
    {
        if (!GatewayCliTestData.IsNativePackageAvailable)
        {
            return;
        }

        using var package = OpenPackage(GatewayCliTestData.HostRuntimePackageId);
        Assert.DoesNotContain(package.Entries, entry =>
            entry.FullName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) ||
            entry.FullName.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase) ||
            entry.FullName.Contains(".dSYM/", StringComparison.OrdinalIgnoreCase));
    }

    [ConditionalFact]
    public void Package_IncludesThirdPartyNotices()
    {
        using var tool = GatewayToolInstallation.Install();

        Assert.True(tool.HasFile("THIRD-PARTY-NOTICES.txt"), "Missing THIRD-PARTY-NOTICES.txt.");
    }

    private static bool PackageHasFile(string packageId, string relativePath)
    {
        using var package = OpenPackage(packageId);
        return package.GetEntry(relativePath) is not null;
    }

    private static string ReadPackageFile(string packageId, string relativePath)
    {
        using var package = OpenPackage(packageId);
        var entry = package.GetEntry(relativePath) ??
            throw new InvalidOperationException($"File '{relativePath}' was not found in package '{packageId}'.");
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    private static ZipArchive OpenPackage(string packageId)
    {
        var packagePath = GatewayCliTestData.TryGetPackagePath(packageId) ??
            throw new InvalidOperationException($"Package '{packageId}' was not built.");
        return ZipFile.OpenRead(packagePath);
    }
}
