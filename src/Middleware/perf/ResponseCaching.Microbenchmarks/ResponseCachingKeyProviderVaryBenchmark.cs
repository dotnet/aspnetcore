// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCaching.Microbenchmarks;

/// <summary>
/// Measures allocations on the vary-by cache-lookup path. The middleware calls
/// <see cref="IResponseCachingKeyProvider.CreateLookupVaryByKeys"/> and enumerates the result
/// to probe the store for a cached vary-by response. The <c>Lookup</c> benchmark exercises that
/// enumerated path; the <c>StorageKeyControl</c> benchmark builds the same underlying storage key
/// without the enumeration wrapper, so the per-call difference is exactly the wrapper cost that
/// this change removes.
/// </summary>
[MemoryDiagnoser]
public class ResponseCachingKeyProviderVaryBenchmark
{
    public enum VaryScenario
    {
        NoVaryControl,
        OneHeaderOneValue,
        OneHeaderMultiValue,
        OneHeaderAbsent,
        ThreeHeaders,
        OneQueryKey,
        WildcardQueryStar,
        HeaderPlusQuery,
        LargeTwentyHeaders,
    }

    private ResponseCachingKeyProvider _keyProvider;
    private ResponseCachingContext _context;

    [Params(
        VaryScenario.NoVaryControl,
        VaryScenario.OneHeaderOneValue,
        VaryScenario.OneHeaderMultiValue,
        VaryScenario.OneHeaderAbsent,
        VaryScenario.ThreeHeaders,
        VaryScenario.OneQueryKey,
        VaryScenario.WildcardQueryStar,
        VaryScenario.HeaderPlusQuery,
        VaryScenario.LargeTwentyHeaders)]
    public VaryScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _keyProvider = new ResponseCachingKeyProvider(new DefaultObjectPoolProvider(), Options.Create(new ResponseCachingOptions()));

        var httpContext = new DefaultHttpContext();
        var rules = new CachedVaryByRules
        {
            VaryByKeyPrefix = "0123456789abcdef0123456789abcdef"
        };

        switch (Scenario)
        {
            case VaryScenario.NoVaryControl:
                break;

            case VaryScenario.OneHeaderOneValue:
                httpContext.Request.Headers["Accept-Encoding"] = "gzip";
                rules.Headers = new[] { "Accept-Encoding" };
                break;

            case VaryScenario.OneHeaderMultiValue:
                httpContext.Request.Headers["Accept-Encoding"] = new[] { "gzip", "br", "deflate" };
                rules.Headers = new[] { "Accept-Encoding" };
                break;

            case VaryScenario.OneHeaderAbsent:
                rules.Headers = new[] { "Accept-Encoding" };
                break;

            case VaryScenario.ThreeHeaders:
                httpContext.Request.Headers["Accept-Encoding"] = "gzip";
                httpContext.Request.Headers["Accept-Language"] = "en-US";
                httpContext.Request.Headers["User-Agent"] = "benchmark";
                rules.Headers = new[] { "Accept-Encoding", "Accept-Language", "User-Agent" };
                break;

            case VaryScenario.OneQueryKey:
                httpContext.Request.QueryString = new QueryString("?culture=en-US");
                rules.QueryKeys = new[] { "culture" };
                break;

            case VaryScenario.WildcardQueryStar:
                httpContext.Request.QueryString = new QueryString("?culture=en-US&theme=dark");
                rules.QueryKeys = new[] { "*" };
                break;

            case VaryScenario.HeaderPlusQuery:
                httpContext.Request.Headers["Accept-Encoding"] = "gzip";
                httpContext.Request.QueryString = new QueryString("?culture=en-US");
                rules.Headers = new[] { "Accept-Encoding" };
                rules.QueryKeys = new[] { "culture" };
                break;

            case VaryScenario.LargeTwentyHeaders:
                var names = new string[20];
                for (var i = 0; i < names.Length; i++)
                {
                    var name = "X-Vary-" + i;
                    names[i] = name;
                    httpContext.Request.Headers[name] = "v" + i;
                }
                rules.Headers = names;
                break;
        }

        _context = new ResponseCachingContext(httpContext, NullLogger.Instance)
        {
            ResponseTime = DateTimeOffset.UtcNow,
            CachedVaryByRules = rules
        };
    }

    [Benchmark]
    public int Lookup()
    {
        var total = 0;
        foreach (var key in _keyProvider.CreateLookupVaryByKeys(_context))
        {
            total += key.Length;
        }

        return total;
    }

    [Benchmark(Baseline = true)]
    public int StorageKeyControl()
    {
        return _keyProvider.CreateStorageVaryByKey(_context).Length;
    }
}
