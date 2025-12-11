// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Antiforgery.Benchmarks.Benchmarks;

/*

    main branch:
    |               Method |     Mean |    Error |   StdDev |     Op/s | Gen 0 | Gen 1 | Gen 2 | Allocated |
    |--------------------- |---------:|---------:|---------:|---------:|------:|------:|------:|----------:|
    |    GetAndStoreTokens | 59.56 us | 2.482 us | 7.082 us | 16,789.6 |     - |     - |     - |      5 KB |
    | ValidateRequestAsync | 50.60 us | 2.150 us | 6.167 us | 19,764.1 |     - |     - |     - |      4 KB |

 */

[AspNetCoreBenchmark]
public class AntiforgeryBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private IAntiforgery _antiforgery = null!;
    private string _cookieName = null!;
    private string _formFieldName = null!;

    // For GetAndStoreTokens - fresh context each time
    private HttpContext _getAndStoreTokensContext = null!;

    // For ValidateRequestAsync - context with valid tokens
    private HttpContext _validateRequestContext = null!;
    private string _cookieToken = null!;
    private string _requestToken = null!;

    [GlobalSetup]
    public void Setup()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddAntiforgery();
        serviceCollection.AddLogging();
        _serviceProvider = serviceCollection.BuildServiceProvider();

        _antiforgery = _serviceProvider.GetRequiredService<IAntiforgery>();

        // Get the actual cookie and form field names from options
        var options = _serviceProvider.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;
        _cookieName = options.Cookie.Name!;
        _formFieldName = options.FormFieldName;

        // Setup context for GetAndStoreTokens (no existing tokens)
        _getAndStoreTokensContext = CreateHttpContext();

        // Generate tokens for validation benchmark
        var tokenContext = CreateHttpContext();
        var tokenSet = _antiforgery.GetAndStoreTokens(tokenContext);
        _cookieToken = tokenSet.CookieToken!;
        _requestToken = tokenSet.RequestToken!;

        // Setup context for ValidateRequestAsync (with valid tokens)
        _validateRequestContext = CreateHttpContextWithTokens(_cookieToken, _requestToken);
    }

    [IterationSetup(Target = nameof(GetAndStoreTokens))]
    public void SetupGetAndStoreTokens()
    {
        // Create a fresh context for each iteration to simulate real-world usage
        _getAndStoreTokensContext = CreateHttpContext();
    }

    [IterationSetup(Target = nameof(ValidateRequestAsync))]
    public void SetupValidateRequest()
    {
        // Create a fresh context with tokens for each iteration
        _validateRequestContext = CreateHttpContextWithTokens(_cookieToken, _requestToken);
    }

    [Benchmark]
    public AntiforgeryTokenSet GetAndStoreTokens()
    {
        return _antiforgery.GetAndStoreTokens(_getAndStoreTokensContext);
    }

    [Benchmark]
    public Task ValidateRequestAsync()
    {
        return _antiforgery.ValidateRequestAsync(_validateRequestContext);
    }

    private HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;

        // Create an authenticated identity with a Name claim (required by antiforgery)
        var identity = new ClaimsIdentity(
            [new Claim(ClaimsIdentity.DefaultNameClaimType, "testuser@example.com")],
            "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";

        // Setup response features to allow cookie writing
        var responseFeature = new TestHttpResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(Stream.Null));

        return context;
    }

    private HttpContext CreateHttpContextWithTokens(string cookieToken, string requestToken)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;

        // Create an authenticated identity with a Name claim (required by antiforgery)
        var identity = new ClaimsIdentity(
            [new Claim(ClaimsIdentity.DefaultNameClaimType, "testuser@example.com")],
            "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";

        // Set the cookie token using the actual cookie name from options
        context.Request.Headers.Cookie = $"{_cookieName}={cookieToken}";

        // Set the request token in form using the actual form field name
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            { _formFieldName, requestToken }
        });

        return context;
    }

    private sealed class TestHttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => false;

        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }
}
