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
        // xUnit 2.x has no runtime skip; tolerate transient feed failures only.
        if (result.LooksLikeNetworkFailure)
        {
            return;
        }

        Assert.True(result.Succeeded, $"Publish should succeed (no 'Sequence contains more than one element').\n{result.Output}");
        Assert.DoesNotContain("Sequence contains more than one element", result.Output);
        Assert.DoesNotContain("Conflicting assets with the same target path", result.Output);

        var routes = GetModulesManifestRoutes(build.Root);
        Assert.Equal("_framework/blazor.modules.json", Assert.Single(routes));

        // The app generated its own manifest (with the RCL module), so the package never
        // materialized its fallback. The module filename is fingerprinted, so match loosely.
        var publishedManifest = FindPublishedFile(build.Root, "blazor.modules.json");
        Assert.NotNull(publishedManifest);
        var publishedContent = File.ReadAllText(publishedManifest!);
        Assert.Contains("_content/rcl/", publishedContent);
        Assert.Contains(".lib.module.js", publishedContent);
    }

    [ConditionalFact]
    public void PublishAndBuild_AppWithoutJsModules_ServesEmptyFallbackModulesManifest()
    {
        using var build = new ConsumerBuild(_output);

        build.CreateProject("app", "app.csproj", AppProject);
        build.CreateFile("app/Program.cs", "class Program { static void Main() { } }");

        var result = build.Run("publish -c Release -v:m", "app/app.csproj");
        // xUnit 2.x has no runtime skip; tolerate transient inability to reach the NuGet feeds.
        if (result.LooksLikeNetworkFailure)
        {
            return;
        }

        Assert.True(result.Succeeded, $"Publish should succeed.\n{result.Output}");
        Assert.DoesNotContain("Conflicting assets with the same target path", result.Output);

        // With no app-provided JS modules, the package materializes its empty ([]) fallback, and it
        // is the single manifest served on the route.
        var routes = GetModulesManifestRoutes(build.Root);
        Assert.Equal("_framework/blazor.modules.json", Assert.Single(routes));

        var publishedManifest = FindPublishedFile(build.Root, "blazor.modules.json");
        Assert.NotNull(publishedManifest);
        Assert.Equal("[]", File.ReadAllText(publishedManifest!).Trim());
    }

    [ConditionalFact]
    public void Publish_ProjectReferenceToWebViewWithJsModuleRcl_SucceedsWithSingleModulesManifest()
    {
        // ProjectReference (P2P) variant of the publish repro. An app references the WebView *source
        // project* (not the package) and contributes JS library modules via an RCL, importing the
        // WebView StaticWebAssets.Groups.targets like the in-repo Photino sample / E2E test. Because
        // the package never materializes its fallback when the app has its own modules, only the
        // app's generated manifest survives on _framework/blazor.modules.json (no conflict, no SDK fix
        // required).
        using var build = new ConsumerBuild(_output, isolateNuGetFeeds: false);

        build.CreateProject("rcl", "rcl.csproj", $"""
            <Project Sdk="Microsoft.NET.Sdk.Razor">
              <PropertyGroup>
                <TargetFramework>{StaticWebAssetsTestData.DefaultTargetFramework}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        build.CreateFile("rcl/wwwroot/rcl.lib.module.js", "export function afterStarted() {}");

        // The app imports the WebView groups targets the same way the in-repo consumers do.
        build.CreateProject("app", "app.csproj", $"""
            <Project Sdk="Microsoft.NET.Sdk.Razor">
              <PropertyGroup>
                <TargetFramework>{StaticWebAssetsTestData.DefaultTargetFramework}</TargetFramework>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{StaticWebAssetsTestData.WebViewProjectPath}" />
                <ProjectReference Include="..\rcl\rcl.csproj" />
              </ItemGroup>
              <Import Project="{StaticWebAssetsTestData.WebViewGroupsTargetsPath}" />
            </Project>
            """);
        build.CreateFile("app/Program.cs", "class Program { static void Main() { } }");

        var result = build.Run("publish -c Release -v:m", "app/app.csproj");
        if (result.LooksLikeNetworkFailure)
        {
            return;
        }

        Assert.True(result.Succeeded, $"Publish should succeed (no 'Conflicting assets').\n{result.Output}");
        Assert.DoesNotContain("Conflicting assets with the same target path", result.Output);

        var routes = GetModulesManifestRoutes(build.Root);
        Assert.Equal("_framework/blazor.modules.json", Assert.Single(routes));

        // The app generated its own manifest (with the RCL module); the WebView fallback was not added.
        var publishedManifest = FindPublishedFile(build.Root, "blazor.modules.json");
        Assert.NotNull(publishedManifest);
        var publishedContent = File.ReadAllText(publishedManifest!);
        Assert.Contains("_content/rcl/", publishedContent);
        Assert.Contains(".lib.module.js", publishedContent);
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
