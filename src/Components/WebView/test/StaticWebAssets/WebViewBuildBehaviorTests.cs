// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.WebView.StaticWebAssets;

/// <summary>
/// End-to-end build/publish tests that reference the locally-built WebView package from a generated
/// app (and an RCL contributing JS library modules) and assert on the produced static web asset
/// endpoints. These reproduce and lock in the fix for issue #67374, where publishing an app that
/// references both the WebView package and a JS-module-contributing RCL crashed with
/// "Sequence contains more than one element".
/// </summary>
[RequiresBuiltPackages("Microsoft.AspNetCore.Components.WebView")]
public class WebViewBuildBehaviorTests
{
    private readonly ITestOutputHelper _output;

    public WebViewBuildBehaviorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string AppProject => $"""
        <Project Sdk="Microsoft.NET.Sdk.Razor">
          <PropertyGroup>
            <TargetFramework>{StaticWebAssetsTestData.DefaultTargetFramework}</TargetFramework>
            <OutputType>Exe</OutputType>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.AspNetCore.Components.WebView" Version="{StaticWebAssetsTestData.PackageVersion}" />
          </ItemGroup>
        </Project>
        """;

    [ConditionalFact]
    public void Publish_AppReferencingRclWithJsModules_ProducesSingleModulesManifest()
    {
        using var build = new ConsumerBuild(_output);

        // RCL that contributes a JS library module, which makes the SDK generate a blazor.modules.json.
        build.CreateProject("rcl", "rcl.csproj", $"""
            <Project Sdk="Microsoft.NET.Sdk.Razor">
              <PropertyGroup>
                <TargetFramework>{StaticWebAssetsTestData.DefaultTargetFramework}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        build.CreateFile("rcl/wwwroot/rcl.lib.module.js", "export function afterStarted() {}");

        // App that references both the RCL and the WebView package (the crashing combination).
        build.CreateProject("app", "app.csproj", $"""
            <Project Sdk="Microsoft.NET.Sdk.Razor">
              <PropertyGroup>
                <TargetFramework>{StaticWebAssetsTestData.DefaultTargetFramework}</TargetFramework>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.WebView" Version="{StaticWebAssetsTestData.PackageVersion}" />
                <ProjectReference Include="..\rcl\rcl.csproj" />
              </ItemGroup>
            </Project>
            """);
        build.CreateFile("app/Program.cs", "class Program { static void Main() { } }");

        var result = build.Run("publish -c Release -v:m", "app/app.csproj");
        // xUnit 2.x has no runtime skip; tolerate transient inability to reach the NuGet feeds.
        if (result.LooksLikeNetworkFailure)
        {
            return;
        }

        Assert.True(result.Succeeded, $"Publish should succeed (no 'Sequence contains more than one element').\n{result.Output}");
        Assert.DoesNotContain("Sequence contains more than one element", result.Output);
        Assert.DoesNotContain("Conflicting assets with the same target path", result.Output);

        var routes = GetModulesManifestRoutes(build.Root);
        Assert.Equal("_framework/blazor.modules.json", Assert.Single(routes));

        // The app's generated manifest (with the RCL module) supersedes the package fallback. The
        // module filename is fingerprinted (e.g. rcl.<hash>.lib.module.js), so match loosely.
        var publishedManifest = FindPublishedFile(build.Root, "blazor.modules.json");
        Assert.NotNull(publishedManifest);
        var publishedContent = File.ReadAllText(publishedManifest!);
        Assert.Contains("_content/rcl/", publishedContent);
        Assert.Contains(".lib.module.js", publishedContent);
    }

    [ConditionalFact]
    public void Build_AppWithoutJsModules_ServesFallbackModulesManifest()
    {
        using var build = new ConsumerBuild(_output);

        build.CreateProject("app", "app.csproj", AppProject);
        build.CreateFile("app/Program.cs", "class Program { static void Main() { } }");

        var result = build.Run("build -c Debug -v:m", "app/app.csproj");
        // xUnit 2.x has no runtime skip; tolerate transient inability to reach the NuGet feeds.
        if (result.LooksLikeNetworkFailure)
        {
            return;
        }

        Assert.True(result.Succeeded, $"Build should succeed.\n{result.Output}");

        // With no app-provided JS modules, the materialized package fallback is served.
        var routes = GetModulesManifestRoutes(build.Root);
        Assert.Equal("_framework/blazor.modules.json", Assert.Single(routes));
    }

    private static string[] GetModulesManifestRoutes(string root)
    {
        var manifestPath = FindFile(root, "app", "app.staticwebassets.endpoints.json")
            ?? throw new InvalidOperationException("Could not find the app's static web assets endpoints manifest.");

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        return doc.RootElement.GetProperty("Endpoints")
            .EnumerateArray()
            .Select(e => e.GetProperty("Route").GetString()!)
            // Ignore fingerprinted routes (e.g. _framework/blazor.<hash>.modules.json); assert on the
            // stable route only.
            .Where(route => route.EndsWith("blazor.modules.json", StringComparison.Ordinal) &&
                            !IsFingerprinted(route))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsFingerprinted(string route)
        => route != "_framework/blazor.modules.json";

    private static string? FindFile(string root, string underDir, string fileName)
        => Directory.EnumerateFiles(Path.Combine(root, underDir), fileName, SearchOption.AllDirectories)
            .FirstOrDefault();

    private static string? FindPublishedFile(string root, string fileName)
        => Directory.EnumerateFiles(Path.Combine(root, "app"), fileName, SearchOption.AllDirectories)
            .FirstOrDefault(p => p.Replace('\\', '/').Contains("/publish/", StringComparison.Ordinal));
}
