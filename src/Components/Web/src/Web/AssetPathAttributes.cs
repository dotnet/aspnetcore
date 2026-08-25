// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Infrastructure for the discovery of HTML attributes that accept static asset-path expansion.
/// </summary>
/// <remarks>
/// To extend the supported element and attribute combinations, define a public class named
/// <c>AssetPathAttributes</c> and annotate it with <see cref="AcceptsAssetPathAttribute"/>.
/// </remarks>
[AcceptsAssetPath("audio", "src")]
[AcceptsAssetPath("img", "src")]
[AcceptsAssetPath("input", "src")]
[AcceptsAssetPath("link", "href")]
[AcceptsAssetPath("script", "src")]
[AcceptsAssetPath("source", "src")]
[AcceptsAssetPath("track", "src")]
[AcceptsAssetPath("video", "poster")]
[AcceptsAssetPath("video", "src")]
public static class AssetPathAttributes
{
}
