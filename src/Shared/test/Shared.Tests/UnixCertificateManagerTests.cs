// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Microsoft.AspNetCore.Certificates.Generation;

namespace Microsoft.AspNetCore.Internal.Tests;

public sealed class UnixCertificateManagerTests : IDisposable
{
    private readonly string _homeDirectory;

    public UnixCertificateManagerTests()
    {
        _homeDirectory = Path.Combine(Path.GetTempPath(), nameof(UnixCertificateManagerTests), Path.GetRandomFileName());
        Directory.CreateDirectory(_homeDirectory);
    }

    [Fact]
    public void Resolve_DiscoversFirefoxProfileFromDefaultXdgConfigDirectory()
    {
        var profileDirectory = CreateDirectory(".config", "mozilla", "firefox", "test.default-release");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: null));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_DiscoversFirefoxProfileFromConfiguredXdgConfigDirectory()
    {
        var xdgConfigHome = CreateDirectory("custom-config");
        var profileDirectory = CreateDirectory("custom-config", "mozilla", "firefox", "test.default-release");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome, nssDbOverride: null));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_IgnoresRelativeXdgConfigDirectory()
    {
        var profileDirectory = CreateDirectory(".config", "mozilla", "firefox", "test.default-release");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, "relative-config", nssDbOverride: null));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_PrefersLegacyFirefoxDirectoryWhenPresent()
    {
        var legacyProfileDirectory = CreateDirectory(".mozilla", "firefox", "legacy.default-release");
        CreateDirectory(".config", "mozilla", "firefox", "xdg.default-release");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: null));

        Assert.Equal(legacyProfileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_RecognizesStandardXdgFirefoxOverride()
    {
        var profileDirectory = CreateDirectory(".config", "mozilla", "firefox", "test.default-release");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: profileDirectory));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_RecognizesConfiguredXdgFirefoxOverride()
    {
        var xdgConfigHome = CreateDirectory("custom-config");
        var profileDirectory = CreateDirectory("custom-config", "mozilla", "firefox", "test.default-release");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome, nssDbOverride: profileDirectory));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_RecognizesTypedFirefoxOverride()
    {
        var profileDirectory = CreateDirectory("custom-browser", "profile");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: $"firefox={profileDirectory}"));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_RecognizesTypedChromiumOverride()
    {
        var profileDirectory = CreateDirectory(".mozilla", "firefox", "test.default-release");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: $"chromium={profileDirectory}"));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertChromiumNssDb(nssDb);
    }

    [Fact]
    public void Resolve_PreservesUntypedChromiumOverride()
    {
        var profileDirectory = CreateDirectory("custom-browser", "profile");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: profileDirectory));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertChromiumNssDb(nssDb);
    }

    [Fact]
    public void Resolve_IgnoresEmptyTypedOverrides()
    {
        var nssDbOverride = $"firefox={Path.PathSeparator}chromium=";

        var nssDbs = UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride);

        Assert.Empty(nssDbs);
    }

    [Fact]
    public void Resolve_OverridesReplaceDiscovery()
    {
        CreateDirectory(".pki", "nssdb");
        var overrideDirectory = CreateDirectory("custom-browser", "profile");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: overrideDirectory));

        Assert.Equal(overrideDirectory, nssDb.Path);
    }

    [Fact]
    public void CreateProcessStartInfos_PreserveFirefoxArgumentsContainingSpaces()
    {
        var profileDirectory = CreateDirectory("custom browser", "profile with spaces");
        var certificatePath = Path.Combine(_homeDirectory, "certificate with spaces.pem");
        const string nickname = "nickname with spaces";
        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(
            _homeDirectory,
            xdgConfigHome: null,
            nssDbOverride: $"firefox={profileDirectory}"));

        AssertProcessStartInfo(
            nssDb.CreateCheckProcessStartInfo(nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-L");
        AssertProcessStartInfo(
            nssDb.CreateAddProcessStartInfo(certificatePath, nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-A", "-i", certificatePath, "-t", "C,,");
        AssertProcessStartInfo(
            nssDb.CreateRemoveProcessStartInfo(nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-D");
    }

    [Fact]
    public void CreateProcessStartInfos_PreserveChromiumArgumentsContainingSpaces()
    {
        var profileDirectory = CreateDirectory("custom browser", "profile with spaces");
        var certificatePath = Path.Combine(_homeDirectory, "certificate with spaces.pem");
        const string nickname = "nickname with spaces";
        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(
            _homeDirectory,
            xdgConfigHome: null,
            nssDbOverride: $"chromium={profileDirectory}"));

        AssertProcessStartInfo(
            nssDb.CreateCheckProcessStartInfo(nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-V", "-u", "V");
        AssertProcessStartInfo(
            nssDb.CreateAddProcessStartInfo(certificatePath, nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-A", "-i", certificatePath, "-t", "P,,");
        AssertProcessStartInfo(
            nssDb.CreateRemoveProcessStartInfo(nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-D");
    }

    public void Dispose()
    {
        Directory.Delete(_homeDirectory, recursive: true);
    }

    private string CreateDirectory(params string[] pathSegments)
    {
        var path = Path.Combine([_homeDirectory, .. pathSegments]);
        Directory.CreateDirectory(path);
        return path;
    }

    private static void AssertFirefoxNssDb(UnixCertificateManager.NssDb nssDb)
    {
        Assert.Equal("Firefox", nssDb.BrowserFamily);
        Assert.Equal(["-L"], nssDb.CheckArguments);
        Assert.Equal("C", nssDb.TrustUsage);
    }

    private static void AssertChromiumNssDb(UnixCertificateManager.NssDb nssDb)
    {
        Assert.Equal("Chromium", nssDb.BrowserFamily);
        Assert.Equal(["-V", "-u", "V"], nssDb.CheckArguments);
        Assert.Equal("P", nssDb.TrustUsage);
    }

    private static void AssertProcessStartInfo(ProcessStartInfo startInfo, params string[] expectedArguments)
    {
        Assert.Equal("certutil", startInfo.FileName);
        Assert.Empty(startInfo.Arguments);
        Assert.Equal(expectedArguments, startInfo.ArgumentList);
    }
}
