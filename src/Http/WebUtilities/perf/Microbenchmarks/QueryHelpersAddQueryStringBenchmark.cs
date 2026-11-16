// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.WebUtilities.Microbenchmarks;

[MemoryDiagnoser]
public class QueryHelpersAddQueryStringBenchmark
{
    private readonly Dictionary<string, string> _oauth = new()
    {
        ["client_id"] = "0123456789abcdef",
        ["scope"] = "openid profile email",
        ["response_type"] = "code",
        ["redirect_uri"] = "https://localhost:5001/signin-oidc",
        ["state"] = "CfDJ8Abcdefghijklmnopqrstuvwxyz0123456789-_ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        ["code_challenge"] = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
        ["code_challenge_method"] = "S256",
    };

    private readonly string _longValue = string.Concat(Enumerable.Repeat("CfDJ8Abcdefghijklmnopqrstuvwxyz0123456789-_", 14));

    [Benchmark]
    public string ReturnUrl() => QueryHelpers.AddQueryString("/Account/Login", "ReturnUrl", "/orders/details?id=42");

    [Benchmark]
    public string Simple() => QueryHelpers.AddQueryString("/api/items", "page", "2");

    [Benchmark]
    public string LongValue() => QueryHelpers.AddQueryString("/signin-oidc", "state", _longValue);

    [Benchmark]
    public string EncodeHeavy() => QueryHelpers.AddQueryString("/search", "q", "caf\u00e9 & cr\u00e8me br\u00fbl\u00e9e / \u4e2d\u6587?#");

    [Benchmark]
    public string OAuth() => QueryHelpers.AddQueryString("https://login.example.com/oauth2/v2.0/authorize", _oauth);
}
