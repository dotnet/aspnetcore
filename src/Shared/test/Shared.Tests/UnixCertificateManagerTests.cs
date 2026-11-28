// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;

using Microsoft.AspNetCore.Certificates.Generation;

namespace Microsoft.AspNetCore.Internal.Tests;

public sealed class UnixCertificateManagerTests : IDisposable
{
    // Real development certificate nicknames are 61 characters, overflowing certutil's 60-column nickname field.
    private const string Nickname = "aspnetcore-localhost-E2BCB3BEB2E06D5F077CA93701892553609702B9";

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
    public void Resolve_DiscoversRegisteredFirefoxProfileWithCustomName()
    {
        var firefoxDirectory = CreateDirectory(".config", "mozilla", "firefox");
        var profileDirectory = CreateDirectory(".config", "mozilla", "firefox", "Profiles", "test.work");
        File.WriteAllText(Path.Combine(firefoxDirectory, "profiles.ini"), """
            [Profile0]
            Name=work
            IsRelative=1
            Path=Profiles/test.work
            """);

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: null));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_DiscoversRegisteredFirefoxProfileWithAbsolutePath()
    {
        var firefoxDirectory = CreateDirectory(".config", "mozilla", "firefox");
        var profileDirectory = CreateDirectory("custom firefox", "profile with spaces");
        File.WriteAllText(Path.Combine(firefoxDirectory, "profiles.ini"), $"""
            [Profile0]
            Name=custom
            IsRelative=0
            Path={profileDirectory}
            """);

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: null));

        Assert.Equal(profileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_SkipsMalformedRegisteredFirefoxPath()
    {
        var firefoxDirectory = CreateDirectory(".config", "mozilla", "firefox");
        var validProfileDirectory = CreateDirectory(".config", "mozilla", "firefox", "Profiles", "valid.work");
        File.WriteAllText(
            Path.Combine(firefoxDirectory, "profiles.ini"),
            """
            [Profile0]
            Name=valid
            IsRelative=1
            Path=Profiles/valid.work

            [Profile1]
            Name=malformed
            IsRelative=1
            Path=Profiles/
            """ + '\0' + "invalid");

        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: null));

        Assert.Equal(validProfileDirectory, nssDb.Path);
        AssertFirefoxNssDb(nssDb);
    }

    [Fact]
    public void Resolve_CombinesDefaultAndRegisteredFirefoxProfiles()
    {
        var firefoxDirectory = CreateDirectory(".config", "mozilla", "firefox");
        var registeredDefaultProfileDirectory = CreateDirectory(".config", "mozilla", "firefox", "registered.default-release");
        var unregisteredDefaultProfileDirectory = CreateDirectory(".config", "mozilla", "firefox", "unregistered.default");
        var registeredProfileDirectory = CreateDirectory(".config", "mozilla", "firefox", "Profiles", "test.work");
        File.WriteAllText(Path.Combine(firefoxDirectory, "profiles.ini"), """
            [Profile0]
            Name=work
            IsRelative=1
            Path=Profiles/test.work

            [Profile1]
            Name=default-release
            IsRelative=1
            Path=registered.default-release
            """);

        var nssDbs = UnixCertificateManager.NssDb.Resolve(_homeDirectory, xdgConfigHome: null, nssDbOverride: null);

        Assert.Equal(
            [registeredProfileDirectory, registeredDefaultProfileDirectory, unregisteredDefaultProfileDirectory],
            nssDbs.Select(nssDb => nssDb.Path).Order(StringComparer.Ordinal));
        Assert.All(nssDbs, AssertFirefoxNssDb);
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
            "-d", $"sql:{profileDirectory}", "-L");
        AssertProcessStartInfo(
            nssDb.CreatePresenceCheckProcessStartInfo(nickname),
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
            nssDb.CreatePresenceCheckProcessStartInfo(nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-L");
        AssertProcessStartInfo(
            nssDb.CreateAddProcessStartInfo(certificatePath, nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-A", "-i", certificatePath, "-t", "P,,");
        AssertProcessStartInfo(
            nssDb.CreateRemoveProcessStartInfo(nickname),
            "-d", $"sql:{profileDirectory}", "-n", nickname, "-D");
    }

    [Theory]
    [InlineData("C,,", true)]
    [InlineData("CTu,u,u", true)]
    [InlineData("P,,", false)]
    [InlineData(",,", false)]
    [InlineData(",C,C", false)]
    public void FirefoxIsTrusted_RequiresTrustedCaSslFlag(string trustAttributes, bool expected)
    {
        var nssDb = CreateFirefoxNssDb();
        var output = FormatCertUtilList(("Other CA", "C,,"), (Nickname, trustAttributes));

        Assert.Equal(expected, nssDb.IsTrusted(Nickname, exitCode: 0, output));
    }

    [Fact]
    public void FirefoxIsTrusted_ReturnsFalseWhenNicknameIsNotListed()
    {
        var nssDb = CreateFirefoxNssDb();
        var output = FormatCertUtilList(($"{Nickname}0", "C,,"), (Nickname[..^1], "C,,"));

        Assert.False(nssDb.IsTrusted(Nickname, exitCode: 0, output));
    }

    [Fact]
    public void FirefoxIsTrusted_MatchesNicknameContainingSpaces()
    {
        var nssDb = CreateFirefoxNssDb();
        var output = FormatCertUtilList(("nickname with spaces", "C,,"));

        Assert.True(nssDb.IsTrusted("nickname with spaces", exitCode: 0, output));
    }

    [Fact]
    public void FirefoxIsTrusted_ReturnsFalseWhenCertUtilFails()
    {
        var nssDb = CreateFirefoxNssDb();
        var output = FormatCertUtilList((Nickname, "C,,"));

        Assert.False(nssDb.IsTrusted(Nickname, exitCode: 255, output));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(255, false)]
    public void ChromiumIsTrusted_UsesValidationExitCode(int exitCode, bool expected)
    {
        var nssDb = Assert.Single(UnixCertificateManager.NssDb.Resolve(
            _homeDirectory,
            xdgConfigHome: null,
            nssDbOverride: $"chromium={CreateDirectory("chromium")}"));

        Assert.Equal(expected, nssDb.IsTrusted(Nickname, exitCode, standardOutput: string.Empty));
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

    private UnixCertificateManager.NssDb CreateFirefoxNssDb()
        => Assert.Single(UnixCertificateManager.NssDb.Resolve(
            _homeDirectory,
            xdgConfigHome: null,
            nssDbOverride: $"firefox={CreateDirectory("firefox")}"));

    // Mirrors the "%-60s %-5s" layout certutil uses when listing an NSS database.
    private static string FormatCertUtilList(params (string Nickname, string TrustAttributes)[] entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"{"Certificate Nickname",-60} {"Trust Attributes",-5}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"{"",-60} {"SSL,S/MIME,JAR/XPI",-5}");
        builder.AppendLine();
        foreach (var (nickname, trustAttributes) in entries)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"{nickname,-60} {trustAttributes,-5}");
        }

        return builder.ToString();
    }

    private static void AssertFirefoxNssDb(UnixCertificateManager.NssDb nssDb)
    {
        Assert.Equal("Firefox", nssDb.BrowserFamily);
        Assert.Equal("C", nssDb.TrustUsage);
    }

    private static void AssertChromiumNssDb(UnixCertificateManager.NssDb nssDb)
    {
        Assert.Equal("Chromium", nssDb.BrowserFamily);
        Assert.Equal("P", nssDb.TrustUsage);
    }

    private static void AssertProcessStartInfo(ProcessStartInfo startInfo, params string[] expectedArguments)
    {
        Assert.Equal("certutil", startInfo.FileName);
        Assert.Empty(startInfo.Arguments);
        Assert.Equal(expectedArguments, startInfo.ArgumentList);
        Assert.Empty(startInfo.InheritedHandles);
    }
}
