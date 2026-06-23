// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.WebView.StaticWebAssets;

/// <summary>
/// Cracks the built .nupkg files and asserts on the static web assets layout/shape for the packages
/// that ship framework or grouped static web assets.
/// </summary>
[RequiresBuiltPackages(
    "Microsoft.AspNetCore.Components.WebView",
    "Microsoft.AspNetCore.Components.WebAssembly",
    "Microsoft.AspNetCore.App.Internal.Assets",
    "Microsoft.AspNetCore.Identity.UI")]
public class PackageLayoutTests
{
    private const string WebViewPackageId = "Microsoft.AspNetCore.Components.WebView";
    private const string WebAssemblyPackageId = "Microsoft.AspNetCore.Components.WebAssembly";
    private const string AssetsInternalPackageId = "Microsoft.AspNetCore.App.Internal.Assets";
    private const string IdentityUIPackageId = "Microsoft.AspNetCore.Identity.UI";

    [ConditionalFact]
    public void WebViewPackage_ShipsFrameworkAssetsUnderFramework()
    {
        using var package = PackageArchive.Open(WebViewPackageId);

        Assert.True(package.HasEntry("staticwebassets/_framework/blazor.modules.json"),
            "blazor.modules.json should ship under staticwebassets/_framework/.");
        Assert.True(package.HasEntry("staticwebassets/_framework/blazor.webview.js"),
            "blazor.webview.js should ship under staticwebassets/_framework/.");
    }

    [ConditionalFact]
    public void WebViewPackage_ModelsBlazorModulesJsonAsFrameworkAsset()
    {
        using var package = PackageArchive.Open(WebViewPackageId);
        using var manifest = package.ReadPackageAssetsManifest();

        var modules = GetAsset(manifest, "_framework/blazor.modules.json");

        // The whole point of the fix (#67374): blazor.modules.json must be a Framework asset so it is
        // materialized into the consuming project, instead of a Package asset that collides with the
        // app's own generated manifest at build/publish time.
        Assert.Equal("Framework", modules.GetProperty("SourceType").GetString());
        Assert.Equal("JSModule", modules.GetProperty("AssetTraitName").GetString());
        Assert.Equal("JSModuleManifest", modules.GetProperty("AssetTraitValue").GetString());
        // No static web asset groups should be involved anymore.
        Assert.True(string.IsNullOrEmpty(modules.GetProperty("AssetGroups").GetString()));
    }

    [ConditionalFact]
    public void WebViewPackage_ModelsBlazorWebViewJsAsFrameworkAsset()
    {
        using var package = PackageArchive.Open(WebViewPackageId);
        using var manifest = package.ReadPackageAssetsManifest();

        var js = GetAsset(manifest, "_framework/blazor.webview.js");

        Assert.Equal("Framework", js.GetProperty("SourceType").GetString());
    }

    [ConditionalFact]
    public void WebViewPackage_ServesModulesManifestAtFrameworkRoute()
    {
        using var package = PackageArchive.Open(WebViewPackageId);
        using var manifest = package.ReadPackageAssetsManifest();

        var routes = manifest.RootElement.GetProperty("Endpoints")
            .EnumerateArray()
            .Select(e => e.GetProperty("Route").GetString())
            .ToArray();

        Assert.Contains("_framework/blazor.modules.json", routes);
        Assert.Contains("_framework/blazor.webview.js", routes);
    }

    [ConditionalFact]
    public void WebViewPackage_GroupsTargetsCarriesConsumerProperties()
    {
        using var package = PackageArchive.Open(WebViewPackageId);

        Assert.True(package.HasEntry("build/StaticWebAssets.Groups.targets"),
            "The package should ship build/StaticWebAssets.Groups.targets with consumer build properties.");

        var groups = package.ReadEntry("build/StaticWebAssets.Groups.targets");
        Assert.Contains("<JSModuleManifestRelativePath", groups);
        Assert.Contains("_framework/blazor.modules.json", groups);
        Assert.Contains("<CompressionEnabled", groups);
    }

    [ConditionalFact]
    public void WebViewPackage_DoesNotUseBlazorWebViewModulesGroupMachinery()
    {
        using var package = PackageArchive.Open(WebViewPackageId);

        // Regression guard for #67374: the deferred BlazorWebViewModules group + manifest-promotion
        // targets are what caused the publish-time crash. They must be gone from every shipped file.
        foreach (var entryName in package.EntryNames.Where(e =>
                     e.EndsWith(".targets", StringComparison.OrdinalIgnoreCase) ||
                     e.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ||
                     e.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            var content = package.ReadEntry(entryName);
            Assert.DoesNotContain("BlazorWebViewModules", content);
            Assert.DoesNotContain("_TagSdkModulesManifestWithGroup", content);
            Assert.DoesNotContain("_ResolveBlazorWebViewModulesGroup", content);
        }
    }

    [ConditionalFact]
    public void WebViewPackage_BuildTargetsImportStaticWebAssetsAndGroups()
    {
        using var package = PackageArchive.Open(WebViewPackageId);

        var buildTargets = package.ReadEntry($"build/{WebViewPackageId}.targets");
        Assert.Contains("Microsoft.AspNetCore.StaticWebAssets.targets", buildTargets);
        Assert.Contains("StaticWebAssets.Groups.targets", buildTargets);

        Assert.True(package.HasEntry($"buildTransitive/{WebViewPackageId}.targets"),
            "buildTransitive targets should be present so the assets flow transitively.");
    }

    [ConditionalFact]
    public void WebAssemblyPackage_ShipsBlazorWebAssemblyAsFrameworkAsset()
    {
        using var package = PackageArchive.Open(WebAssemblyPackageId);

        Assert.True(package.HasEntry("staticwebassets/_framework/blazor.webassembly.js"));

        using var manifest = package.ReadPackageAssetsManifest();
        var js = GetAsset(manifest, "_framework/blazor.webassembly.js");
        Assert.Equal("Framework", js.GetProperty("SourceType").GetString());
    }

    [ConditionalFact]
    public void AssetsInternalPackage_ShipsBlazorScriptsAsFrameworkAssets()
    {
        using var package = PackageArchive.Open(AssetsInternalPackageId);

        Assert.True(package.HasEntry("staticwebassets/_framework/blazor.web.js"));
        Assert.True(package.HasEntry("staticwebassets/_framework/blazor.server.js"));

        using var manifest = package.ReadPackageAssetsManifest();
        Assert.Equal("Framework", GetAsset(manifest, "_framework/blazor.web.js").GetProperty("SourceType").GetString());
        Assert.Equal("Framework", GetAsset(manifest, "_framework/blazor.server.js").GetProperty("SourceType").GetString());
    }

    [ConditionalFact]
    public void IdentityUIPackage_ShipsBootstrapAssetsForBothVersions()
    {
        using var package = PackageArchive.Open(IdentityUIPackageId);

        Assert.Contains(package.EntryNames, e => e.StartsWith("staticwebassets/V4/lib/bootstrap/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(package.EntryNames, e => e.StartsWith("staticwebassets/V5/lib/bootstrap/", StringComparison.OrdinalIgnoreCase));
    }

    [ConditionalFact]
    public void IdentityUIPackage_GroupsTargetsSelectsBootstrapVersion()
    {
        using var package = PackageArchive.Open(IdentityUIPackageId);

        Assert.True(package.HasEntry("build/StaticWebAssets.Groups.targets"));
        var groups = package.ReadEntry("build/StaticWebAssets.Groups.targets");
        Assert.Contains("BootstrapVersion", groups);
    }

    private static JsonElement GetAsset(JsonDocument manifest, string relativePathSuffix)
    {
        foreach (var asset in manifest.RootElement.GetProperty("Assets").EnumerateObject())
        {
            if (asset.Name.Replace('\\', '/').EndsWith(relativePathSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return asset.Value;
            }
        }

        throw new InvalidOperationException($"No asset ending with '{relativePathSuffix}' found in the package manifest.");
    }
}
