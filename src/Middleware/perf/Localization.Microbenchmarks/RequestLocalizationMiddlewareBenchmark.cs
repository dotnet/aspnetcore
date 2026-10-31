// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Localization.Microbenchmarks;

[MemoryDiagnoser]
public class RequestLocalizationMiddlewareBenchmark
{
    private RequestLocalizationMiddleware _middleware = default!;
    private HttpContext _acceptLanguageContext = default!;
    private HttpContext _queryStringContext = default!;
    private HttpContext _noCultureContext = default!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new RequestLocalizationOptions()
            .SetDefaultCulture("en-US")
            .AddSupportedCultures("en-US", "fr-FR", "de-DE", "es-ES")
            .AddSupportedUICultures("en-US", "fr-FR", "de-DE", "es-ES");

        _middleware = new RequestLocalizationMiddleware(
            _ => Task.CompletedTask,
            Options.Create(options),
            NullLoggerFactory.Instance);

        _acceptLanguageContext = new DefaultHttpContext();
        _acceptLanguageContext.Request.Headers.AcceptLanguage = "fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7";

        _queryStringContext = new DefaultHttpContext();
        _queryStringContext.Request.QueryString = new QueryString("?culture=de-DE&ui-culture=de-DE");

        _noCultureContext = new DefaultHttpContext();
    }

    [Benchmark]
    public Task AcceptLanguageHeader() => _middleware.Invoke(_acceptLanguageContext);

    [Benchmark]
    public Task QueryString() => _middleware.Invoke(_queryStringContext);

    [Benchmark]
    public Task NoCulture() => _middleware.Invoke(_noCultureContext);
}
