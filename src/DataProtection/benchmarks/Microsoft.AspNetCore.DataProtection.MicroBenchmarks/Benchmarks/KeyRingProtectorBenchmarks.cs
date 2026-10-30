// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
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

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("/home/site/wwwroot/");
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IDataProtectionProvider>();

        _shortPurposeProtector = provider.CreateProtector("benchmark");
        // Mirrors the purpose chain used by cookie authentication.
        _cookiePurposeProtector = provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", "Cookies", "v2");

        _plaintext = new byte[200];
        new Random(42).NextBytes(_plaintext);

        _shortProtected = _shortPurposeProtector.Protect(_plaintext);
        _cookieProtected = _cookiePurposeProtector.Protect(_plaintext);
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
