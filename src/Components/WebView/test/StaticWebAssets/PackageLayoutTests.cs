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
    public void WebViewPackage_ShipsStaticWebAssets()
    {
        using var package = PackageArchive.Open(WebViewPackageId);

        Assert.True(package.HasEntry("staticwebassets/blazor.webview.js"),
            "blazor.webview.js should ship under staticwebassets/ as a package static web asset.");
        // The fallback blazor.modules.json ships raw under build/ (NOT as a static web asset), so it
        // never auto-flows and never collides with the SDK-generated manifest. It is materialized
        // conditionally by StaticWebAssets.Groups.targets.
        Assert.True(package.HasEntry("build/blazor.modules.json"),
            "blazor.modules.json should ship raw under build/.");
        Assert.False(package.HasEntry("staticwebassets/blazor.modules.json"),
            "blazor.modules.json should NOT ship under staticwebassets/ (it is not a static web asset).");
    }

    [ConditionalFact]
    public void WebViewPackage_ShipsBlazorModulesJsonAsRawEmptyFallback()
    {
        using var package = PackageArchive.Open(WebViewPackageId);
        using var manifest = package.ReadPackageAssetsManifest();

        // blazor.modules.json is NOT a package static web asset: it is not present in the package
        // assets manifest, so it does not auto-flow to consumers and cannot conflict with the
        // SDK-generated _framework/blazor.modules.json when the app has its own JS modules.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("Assets").EnumerateObject(),
            asset => asset.Name.Replace('\\', '/').EndsWith("blazor.modules.json", StringComparison.OrdinalIgnoreCase));

        // The raw fallback shipped under build/ is the empty module manifest.
        var fallback = package.ReadEntry("build/blazor.modules.json").Trim();
        Assert.Equal("[]", fallback);
    }

    [ConditionalFact]
    public void WebViewPackage_ModelsBlazorWebViewJsAsPackageAsset()
    {
        using var package = PackageArchive.Open(WebViewPackageId);
        using var manifest = package.ReadPackageAssetsManifest();

        var js = GetAsset(manifest, "blazor.webview.js");

        Assert.Equal("Package", js.GetProperty("SourceType").GetString());
        Assert.Equal("_framework", js.GetProperty("BasePath").GetString());
    }

    [ConditionalFact]
    public void WebViewPackage_ServesWebViewJsAtFrameworkRoute()
    {
        using var package = PackageArchive.Open(WebViewPackageId);
        using var manifest = package.ReadPackageAssetsManifest();

        var routes = manifest.RootElement.GetProperty("Endpoints")
            .EnumerateArray()
            .Select(e => e.GetProperty("Route").GetString())
            .ToArray();

        Assert.Contains("_framework/blazor.webview.js", routes);
        // The fallback modules manifest is not a package static web asset, so it has no package endpoint.
        Assert.DoesNotContain("_framework/blazor.modules.json", routes);
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
    public void WebViewPackage_MaterializesModulesFallbackConditionally()
    {
        using var package = PackageArchive.Open(WebViewPackageId);

        // The fallback manifest is materialized as the consumer's own static web asset only when the
        // app contributes no JS library modules of its own (decided before the build manifest is
        // generated, so there is never a conflict). No deferred group / consumer-manifest tagging is
        // involved.
        var groups = package.ReadEntry("build/StaticWebAssets.Groups.targets");
        Assert.Contains("_AddBlazorWebViewModulesFallback", groups);
        Assert.Contains("_ExistingBuildJSModules", groups);
        Assert.Contains("ResolveStaticWebAssetsInputsDependsOn", groups);

        // Lock in the simplification: no asset groups, no tagging/promotion of the SDK manifest.
        Assert.DoesNotContain("Deferred=\"true\"", groups);
        Assert.DoesNotContain("_TagSdkModulesManifestWithGroup", groups);
        Assert.DoesNotContain("StaticWebAssetGroup", groups);

        var manifest = package.ReadEntry($"build/{WebViewPackageId}.PackageAssets.json");
        Assert.DoesNotContain("BlazorWebViewModules", manifest);
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
