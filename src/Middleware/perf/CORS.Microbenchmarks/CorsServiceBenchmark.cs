// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public class CorsServiceBenchmark
{
    private CorsService _corsService = default!;
    private CorsPolicy _policy = default!;
    private DefaultHttpContext _actualRequest = default!;
    private DefaultHttpContext _preflightRequest = default!;

    [GlobalSetup]
    public void Setup()
    {
        _corsService = new CorsService(Options.Create(new CorsOptions()), NullLoggerFactory.Instance);
        _policy = new CorsPolicyBuilder()
            .WithOrigins("https://contoso.com", "https://example.com", "https://fabrikam.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .Build();

        _actualRequest = new DefaultHttpContext();
        _actualRequest.Request.Method = HttpMethods.Get;
        _actualRequest.Request.Headers.Origin = "https://fabrikam.com";

        _preflightRequest = new DefaultHttpContext();
        _preflightRequest.Request.Method = HttpMethods.Options;
        _preflightRequest.Request.Headers.Origin = "https://fabrikam.com";
        _preflightRequest.Request.Headers.AccessControlRequestMethod = "PUT";
        _preflightRequest.Request.Headers.AccessControlRequestHeaders = "content-type, x-custom";
    }

    [Benchmark]
    public CorsResult EvaluateActualRequest() => _corsService.EvaluatePolicy(_actualRequest, _policy);

    [Benchmark]
    public void EvaluateAndApplyActualRequest()
    {
        var result = _corsService.EvaluatePolicy(_actualRequest, _policy);
        _actualRequest.Response.Headers.Clear();
        _corsService.ApplyResult(result, _actualRequest.Response);
    }

    [Benchmark]
    public void EvaluateAndApplyPreflightRequest()
    {
        var result = _corsService.EvaluatePolicy(_preflightRequest, _policy);
        _preflightRequest.Response.Headers.Clear();
        _corsService.ApplyResult(result, _preflightRequest.Response);
    }
}
