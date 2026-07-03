// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Cracks the built <c>Microsoft.AspNetCore.Components.Gateway.Cli</c> .nupkg and asserts it is a
/// well-formed dotnet tool package that repackages the gateway binaries under the
/// <c>blazor-gateway</c> command.
/// </summary>
[RequiresBuiltGatewayCliPackage]
public class BlazorGatewayCliPackageTests
{
    private static string ToolDir => $"tools/{GatewayCliTestData.DefaultTargetFramework}/any";

    [ConditionalFact]
    public void Package_IsMarkedAsDotnetTool()
    {
        using var package = GatewayToolPackageArchive.Open();

        var nuspec = XDocument.Parse(package.ReadNuspec());
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
    public void Package_ContainsDotnetToolSettings_ForBlazorGatewayCommand()
    {
        using var package = GatewayToolPackageArchive.Open();

        Assert.True(
            package.HasEntry($"{ToolDir}/DotnetToolSettings.xml"),
            $"Expected DotnetToolSettings.xml under {ToolDir}.");

        var settings = XDocument.Parse(package.ReadEntry($"{ToolDir}/DotnetToolSettings.xml"));
        var command = settings.Descendants("Command").Single();

        Assert.Equal(GatewayCliTestData.ToolCommandName, command.Attribute("Name")?.Value);
        Assert.Equal("blazor-gateway.dll", command.Attribute("EntryPoint")?.Value);
        Assert.Equal("dotnet", command.Attribute("Runner")?.Value);
    }

    [ConditionalFact]
    public void Package_ContainsGatewayBinariesAndRuntimeConfig()
    {
        using var package = GatewayToolPackageArchive.Open();

        // The entry point plus the framework-dependent app files that make it runnable.
        Assert.True(package.HasEntry($"{ToolDir}/blazor-gateway.dll"), "Missing blazor-gateway.dll.");
        Assert.True(package.HasEntry($"{ToolDir}/blazor-gateway.deps.json"), "Missing blazor-gateway.deps.json.");
        Assert.True(
            package.HasEntry($"{ToolDir}/blazor-gateway.runtimeconfig.json"),
            "Missing blazor-gateway.runtimeconfig.json (the gateway emits it into the build output; the tool package must copy it in).");
    }

    [ConditionalFact]
    public void Package_RuntimeConfig_TargetsAspNetCoreSharedFramework()
    {
        using var package = GatewayToolPackageArchive.Open();

        var runtimeConfig = package.ReadEntry($"{ToolDir}/blazor-gateway.runtimeconfig.json");

        // The gateway is a framework-dependent app; the tool must roll forward onto the installed
        // Microsoft.AspNetCore.App shared framework rather than carrying its own runtime.
        Assert.Contains("Microsoft.AspNetCore.App", runtimeConfig, StringComparison.Ordinal);
    }

    [ConditionalFact]
    public void Package_IncludesBundledDependencies()
    {
        using var package = GatewayToolPackageArchive.Open();

        // The gateway depends on YARP for reverse proxying; it must travel inside the tool package
        // because a dotnet tool cannot pull additional package references at run time.
        Assert.True(package.HasEntry($"{ToolDir}/Yarp.ReverseProxy.dll"), "Missing bundled Yarp.ReverseProxy.dll.");
    }

    [ConditionalFact]
    public void Package_IncludesThirdPartyNotices()
    {
        using var package = GatewayToolPackageArchive.Open();

        Assert.True(package.HasEntry("THIRD-PARTY-NOTICES.txt"), "Missing THIRD-PARTY-NOTICES.txt.");
    }
}
