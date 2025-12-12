// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Antiforgery.Benchmarks.Benchmarks;

/*
    main branch:
    |                             Method |        Mean |     Error |    StdDev |            Op/s |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |----------------------------------- |------------:|----------:|----------:|----------------:|-------:|------:|------:|----------:|
    |     GenerateRequestToken_Anonymous |  11.0555 ns | 0.1203 ns | 0.1066 ns |    90,452,434.9 | 0.0007 |     - |     - |      56 B |
    | GenerateRequestToken_Authenticated | 401.2545 ns | 7.1693 ns | 6.3554 ns |     2,492,184.2 | 0.0076 |     - |     - |     592 B |
    |      TryValidateTokenSet_Anonymous |   6.7227 ns | 0.0357 ns | 0.0316 ns |   148,750,552.9 |      - |     - |     - |         - |
    |  TryValidateTokenSet_Authenticated | 508.1742 ns | 4.4728 ns | 3.7350 ns |     1,967,829.1 | 0.0095 |     - |     - |     760 B |
    |    TryValidateTokenSet_ClaimsBased | 308.4674 ns | 3.3256 ns | 3.1108 ns |     3,241,833.1 | 0.0038 |     - |     - |     312 B |

    this PR:
    |                             Method |       Mean |      Error |     StdDev |     Median |          Op/s |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |----------------------------------- |-----------:|-----------:|-----------:|-----------:|--------------:|-------:|------:|------:|----------:|
    |     GenerateRequestToken_Anonymous |  17.814 ns |  0.3614 ns |  0.4017 ns |  17.771 ns |  56,134,225.3 | 0.0007 |     - |     - |      56 B |
    | GenerateRequestToken_Authenticated | 459.848 ns |  9.1450 ns |  9.7851 ns | 461.283 ns |   2,174,632.5 | 0.0052 |     - |     - |     424 B |
    |      TryValidateTokenSet_Anonymous |   9.784 ns |  0.6728 ns |  1.9837 ns |  10.357 ns | 102,207,305.5 |      - |     - |     - |         - |
    |  TryValidateTokenSet_Authenticated |  17.300 ns |  0.4886 ns |  1.4407 ns |  17.929 ns |  57,804,630.5 |      - |     - |     - |         - |
    |    TryValidateTokenSet_ClaimsBased | 380.134 ns | 12.9953 ns | 38.3168 ns | 394.899 ns |   2,630,649.7 | 0.0014 |     - |     - |     120 B |

 */

[AspNetCoreBenchmark]
public class AntiforgeryTokenGeneratorBenchmarks
{
    private IAntiforgeryTokenGenerator _tokenGenerator = null!;

    // Anonymous user scenario
    private HttpContext _anonymousHttpContext = null!;
    private AntiforgeryToken _anonymousCookieToken = null!;
    private AntiforgeryToken _anonymousRequestToken = null!;

    // Authenticated user with username scenario
    private HttpContext _authenticatedHttpContext = null!;
    private AntiforgeryToken _authenticatedCookieToken = null!;
    private AntiforgeryToken _authenticatedRequestToken = null!;

    // Claims-based user scenario
    private HttpContext _claimsHttpContext = null!;
    private AntiforgeryToken _claimsCookieToken = null!;
    private AntiforgeryToken _claimsRequestToken = null!;

    [GlobalSetup]
    public void Setup()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddAntiforgery();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _tokenGenerator = serviceProvider.GetRequiredService<IAntiforgeryTokenGenerator>();

        // Setup anonymous user scenario
        _anonymousHttpContext = new DefaultHttpContext();
        _anonymousHttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        _anonymousCookieToken = new AntiforgeryToken { IsCookieToken = true };
        _anonymousRequestToken = new AntiforgeryToken
        {
            IsCookieToken = false,
            SecurityToken = _anonymousCookieToken.SecurityToken,
            Username = string.Empty
        };

        // Setup authenticated user with username scenario
        _authenticatedHttpContext = new DefaultHttpContext();
        var authenticatedIdentity = new ClaimsIdentity(
            [new Claim(ClaimsIdentity.DefaultNameClaimType, "testuser@example.com")],
            "TestAuthentication");
        _authenticatedHttpContext.User = new ClaimsPrincipal(authenticatedIdentity);

        _authenticatedCookieToken = new AntiforgeryToken { IsCookieToken = true };
        _authenticatedRequestToken = new AntiforgeryToken
        {
            IsCookieToken = false,
            SecurityToken = _authenticatedCookieToken.SecurityToken,
            Username = "testuser@example.com"
        };

        // Setup claims-based user scenario
        _claimsHttpContext = new DefaultHttpContext();
        var claimsIdentity = new ClaimsIdentity(
            [
                new Claim(ClaimsIdentity.DefaultNameClaimType, "claimsuser@example.com"),
                new Claim("sub", "user-id-12345"),
                new Claim(ClaimTypes.NameIdentifier, "unique-id")
            ],
            "ClaimsAuthentication");
        _claimsHttpContext.User = new ClaimsPrincipal(claimsIdentity);

        _claimsCookieToken = new AntiforgeryToken { IsCookieToken = true };

        // For claims-based users, we need to extract the ClaimUid
        var claimUid = new byte[32];
        _ = ClaimUidExtractor.TryExtractClaimUidBytes(_claimsHttpContext.User, claimUid);
        _claimsRequestToken = new AntiforgeryToken
        {
            IsCookieToken = false,
            SecurityToken = _claimsCookieToken.SecurityToken,
            ClaimUid = claimUid is not null ? new BinaryBlob(256, claimUid) : null
        };
    }

    [Benchmark]
    public object GenerateRequestToken_Anonymous()
    {
        return _tokenGenerator.GenerateRequestToken(_anonymousHttpContext, _anonymousCookieToken);
    }

    [Benchmark]
    public object GenerateRequestToken_Authenticated()
    {
        return _tokenGenerator.GenerateRequestToken(_authenticatedHttpContext, _authenticatedCookieToken);
    }

    [Benchmark]
    public bool TryValidateTokenSet_Anonymous()
    {
        return _tokenGenerator.TryValidateTokenSet(
            _anonymousHttpContext,
            _anonymousCookieToken,
            _anonymousRequestToken,
            out _);
    }

    [Benchmark]
    public bool TryValidateTokenSet_Authenticated()
    {
        return _tokenGenerator.TryValidateTokenSet(
            _authenticatedHttpContext,
            _authenticatedCookieToken,
            _authenticatedRequestToken,
            out _);
    }

    [Benchmark]
    public bool TryValidateTokenSet_ClaimsBased()
    {
        return _tokenGenerator.TryValidateTokenSet(
            _claimsHttpContext,
            _claimsCookieToken,
            _claimsRequestToken,
            out _);
    }
}
