// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

public class WebHostBuilderDirectTlsExtensionsTests
{
    [Fact]
    public void GetKestrelCoreVersionMismatchError_ReturnsNull_WhenMajorsMatch()
    {
        // Prerelease/patch differences within the same major must be tolerated (framework roll-forward).
        var error = WebHostBuilderDirectTlsExtensions.GetKestrelCoreVersionMismatchError(
            "11.0.0-dev", "11.0.5-preview.6.25361.1+abc", "DirectTls", "Kestrel.Core");

        Assert.Null(error);
    }

    [Fact]
    public void GetKestrelCoreVersionMismatchError_ReturnsError_WhenMajorsDiffer()
    {
        var error = WebHostBuilderDirectTlsExtensions.GetKestrelCoreVersionMismatchError(
            "11.0.0-dev", "12.0.0-dev", "DirectTls", "Kestrel.Core");

        Assert.NotNull(error);
        Assert.Contains("11.0.0-dev", error);
        Assert.Contains("12.0.0-dev", error);
    }

    [Theory]
    [InlineData(null, "11.0.0-dev")]
    [InlineData("11.0.0-dev", null)]
    [InlineData("", "11.0.0-dev")]
    [InlineData("not-a-version", "11.0.0-dev")]
    public void GetKestrelCoreVersionMismatchError_ReturnsNull_WhenVersionUnparseable(string? directTlsVersion, string? kestrelCoreVersion)
    {
        // Fail-open: an unreadable version is not evidence of a mismatch, so it must not block startup.
        Assert.Null(WebHostBuilderDirectTlsExtensions.GetKestrelCoreVersionMismatchError(
            directTlsVersion, kestrelCoreVersion, "DirectTls", "Kestrel.Core"));
    }

    [Fact]
    public void DirectTlsAndKestrelCore_ShipWithMatchingProductMajor()
    {
        // The real assemblies built in this repo must pass the startup check. AssemblyVersion diverges here
        // (Kestrel.Core is shared-framework-pinned, DirectTls keeps the dev sentinel), so only the product
        // (informational) version is a valid comparison - which is exactly what the check uses.
        var directTls = typeof(WebHostBuilderDirectTlsExtensions).Assembly;
        var kestrelCore = typeof(KestrelServerOptions).Assembly;

        Assert.Null(WebHostBuilderDirectTlsExtensions.GetKestrelCoreVersionMismatchError(
            directTls.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            kestrelCore.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            directTls.GetName().Name,
            kestrelCore.GetName().Name));
    }
}
