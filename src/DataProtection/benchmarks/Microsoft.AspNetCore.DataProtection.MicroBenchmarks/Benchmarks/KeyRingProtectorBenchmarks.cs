// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection.MicroBenchmarks.Benchmarks;

[MemoryDiagnoser]
public class KeyRingProtectorBenchmarks
{
    private IDataProtector _shortPurposeProtector = null!;
    private IDataProtector _cookiePurposeProtector = null!;
    private byte[] _plaintext = null!;
    private byte[] _shortProtected = null!;
    private byte[] _cookieProtected = null!;
    private DirectoryInfo _keyDirectory = null!;
    private ServiceProvider _serviceProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Use an isolated key ring with a managed encryptor so ManagedAuthenticatedEncryptor is measured on every OS
        // (on Windows the default algorithms map to CNG) and existing keys in the user profile are not reused.
        _keyDirectory = Directory.CreateTempSubdirectory();
        var services = new ServiceCollection();
        services.AddDataProtection()
            .SetApplicationName("/home/site/wwwroot/")
            .UseCustomCryptographicAlgorithms(new ManagedAuthenticatedEncryptorConfiguration())
            .PersistKeysToFileSystem(_keyDirectory);
        _serviceProvider = services.BuildServiceProvider();
        var provider = _serviceProvider.GetRequiredService<IDataProtectionProvider>();

        _shortPurposeProtector = provider.CreateProtector("benchmark");
        // Mirrors the purpose chain used by cookie authentication.
        _cookiePurposeProtector = provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", "Cookies", "v2");

        _plaintext = new byte[200];
        new Random(42).NextBytes(_plaintext);

        _shortProtected = _shortPurposeProtector.Protect(_plaintext);
        _cookieProtected = _cookiePurposeProtector.Protect(_plaintext);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider.Dispose();
        _keyDirectory.Delete(recursive: true);
    }

    [Benchmark]
    public byte[] Protect_ShortPurpose() => _shortPurposeProtector.Protect(_plaintext);

    [Benchmark]
    public byte[] Unprotect_ShortPurpose() => _shortPurposeProtector.Unprotect(_shortProtected);

    [Benchmark]
    public byte[] Protect_CookiePurpose() => _cookiePurposeProtector.Protect(_plaintext);

    [Benchmark]
    public byte[] Unprotect_CookiePurpose() => _cookiePurposeProtector.Unprotect(_cookieProtected);
}
