// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Installs the built <c>Microsoft.AspNetCore.Components.Gateway.Cli</c> package as a dotnet tool and
/// asserts it is a well-formed tool that repackages the gateway binaries under the
/// <c>blazor-gateway</c> command. These tests install into a throwaway tool-path rather than reading
/// the .nupkg directly, so they exercise the same acquisition path as a real consumer.
/// </summary>
[RequiresBuiltGatewayCliPackage]
public class BlazorGatewayCliPackageTests
{
    private static string ToolDir => $"tools/{GatewayCliTestData.DefaultTargetFramework}/any";

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
    public void Package_ContainsDotnetToolSettings_ForBlazorGatewayCommand()
    {
        using var tool = GatewayToolInstallation.Install();

        Assert.True(
            tool.HasFile($"{ToolDir}/DotnetToolSettings.xml"),
            $"Expected DotnetToolSettings.xml under {ToolDir}.");

        var settings = XDocument.Parse(tool.ReadFile($"{ToolDir}/DotnetToolSettings.xml"));
        var command = settings.Descendants("Command").Single();

        Assert.Equal(GatewayCliTestData.ToolCommandName, command.Attribute("Name")?.Value);
        Assert.Equal("blazor-gateway.dll", command.Attribute("EntryPoint")?.Value);
        Assert.Equal("dotnet", command.Attribute("Runner")?.Value);
    }

    [ConditionalFact]
    public void Package_ContainsGatewayBinariesAndRuntimeConfig()
    {
        using var tool = GatewayToolInstallation.Install();

        // The entry point plus the framework-dependent app files that make it runnable.
        Assert.True(tool.HasFile($"{ToolDir}/blazor-gateway.dll"), "Missing blazor-gateway.dll.");
        Assert.True(tool.HasFile($"{ToolDir}/blazor-gateway.deps.json"), "Missing blazor-gateway.deps.json.");
        Assert.True(
            tool.HasFile($"{ToolDir}/blazor-gateway.runtimeconfig.json"),
            "Missing blazor-gateway.runtimeconfig.json (the gateway emits it into the build output; the tool package must copy it in).");
    }

    [ConditionalFact]
    public void Package_RuntimeConfig_TargetsAspNetCoreSharedFramework()
    {
        using var tool = GatewayToolInstallation.Install();

        var runtimeConfig = tool.ReadFile($"{ToolDir}/blazor-gateway.runtimeconfig.json");

        // The gateway is a framework-dependent app; the tool must roll forward onto the installed
        // Microsoft.AspNetCore.App shared framework rather than carrying its own runtime.
        Assert.Contains("Microsoft.AspNetCore.App", runtimeConfig, StringComparison.Ordinal);
    }

    [ConditionalFact]
    public void Package_IncludesBundledDependencies()
    {
        using var tool = GatewayToolInstallation.Install();

        // The gateway depends on YARP for reverse proxying; it must travel inside the tool package
        // because a dotnet tool cannot pull additional package references at run time.
        Assert.True(tool.HasFile($"{ToolDir}/Yarp.ReverseProxy.dll"), "Missing bundled Yarp.ReverseProxy.dll.");
    }

    [ConditionalFact]
    public void Package_IncludesThirdPartyNotices()
    {
        using var tool = GatewayToolInstallation.Install();

        Assert.True(tool.HasFile("THIRD-PARTY-NOTICES.txt"), "Missing THIRD-PARTY-NOTICES.txt.");
    }
}
