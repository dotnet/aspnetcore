// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Antiforgery.Microbenchmarks.Benchmarks;

[AspNetCoreBenchmark]
public class AntiforgeryBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private IAntiforgery _antiforgery = null!;
    private string _cookieName = null!;
    private string _formFieldName = null!;

    // Reusable contexts - reset between iterations instead of recreating
    private DefaultHttpContext _getAndStoreTokensContext = null!;
    private DefaultHttpContext _validateRequestContext = null!;
    private TestHttpResponseFeature _getAndStoreTokensResponseFeature = null!;

    // Pre-generated tokens for validation benchmark
    private string _cookieToken = null!;
    private string _requestToken = null!;

    // Pre-allocated form collection for validation benchmark
    private FormCollection _validationFormCollection = null!;

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

        // Create reusable context for GetAndStoreTokens
        _getAndStoreTokensResponseFeature = new TestHttpResponseFeature();
        _getAndStoreTokensContext = CreateHttpContext(_getAndStoreTokensResponseFeature);

        // Generate tokens for validation benchmark
        var tokenContext = CreateHttpContext(new TestHttpResponseFeature());
        var tokenSet = _antiforgery.GetAndStoreTokens(tokenContext);
        _cookieToken = tokenSet.CookieToken!;
        _requestToken = tokenSet.RequestToken!;

        // Pre-allocate form collection for validation
        _validationFormCollection = new FormCollection(new Dictionary<string, StringValues>
        {
            { _formFieldName, _requestToken }
        });

        // Create reusable context for ValidateRequestAsync
        _validateRequestContext = CreateHttpContextWithTokens();
    }

    [IterationSetup(Target = nameof(GetAndStoreTokens))]
    public void SetupGetAndStoreTokens()
    {
        // Reset the context instead of creating a new one
        ResetHttpContextForGetAndStoreTokens();
    }

    [IterationSetup(Target = nameof(ValidateRequestAsync))]
    public void SetupValidateRequest()
    {
        // Reset the context instead of creating a new one
        ResetHttpContextForValidation();
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

    private DefaultHttpContext CreateHttpContext(TestHttpResponseFeature responseFeature)
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
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(Stream.Null));

        return context;
    }

    private DefaultHttpContext CreateHttpContextWithTokens()
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
        context.Request.Headers.Cookie = $"{_cookieName}={_cookieToken}";

        // Set the request token in form using the pre-allocated form collection
        context.Request.Form = _validationFormCollection;

        return context;
    }

    private void ResetHttpContextForGetAndStoreTokens()
    {
        // Clear the antiforgery feature so it generates fresh tokens
        _getAndStoreTokensContext.Features.Set<IAntiforgeryFeature>(null);

        // Reset response headers that antiforgery sets
        _getAndStoreTokensResponseFeature.Headers.Clear();
    }

    private void ResetHttpContextForValidation()
    {
        // Clear the antiforgery feature so it deserializes tokens fresh
        _validateRequestContext.Features.Set<IAntiforgeryFeature>(null);
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
