// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Builder.Internal;

public class ApplicationBuilderTests
{
    [Fact]
    public void BuildReturnsCallableDelegate()
    {
        var builder = new ApplicationBuilder(null);
        var app = builder.Build();

        var httpContext = new DefaultHttpContext();

        app.Invoke(httpContext);
        Assert.Equal(404, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task BuildReturnDelegateThatDoesNotSetStatusCodeIfResponseHasStarted()
    {
        var builder = new ApplicationBuilder(null);
        var app = builder.Build();

        var httpContext = new DefaultHttpContext();
        var responseFeature = new TestHttpResponseFeature();
        httpContext.Features.Set<IHttpResponseFeature>(responseFeature);
        httpContext.Response.StatusCode = 200;

        responseFeature.HasStarted = true;

        await app.Invoke(httpContext);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public void ServerFeaturesEmptyWhenNotSpecified()
    {
        var builder = new ApplicationBuilder(null);

        Assert.Empty(builder.ServerFeatures);
    }

    [Fact]
    public async Task BuildImplicitlyThrowsForMatchedEndpointAsLastStep()
    {
        var builder = new ApplicationBuilder(null);
        var app = builder.Build();

        var endpointCalled = false;
        var endpoint = new Endpoint(
            context =>
            {
                endpointCalled = true;
                return Task.CompletedTask;
            },
            EndpointMetadataCollection.Empty,
            "Test endpoint");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(endpoint);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => app.Invoke(httpContext));

        var expected =
            "The request reached the end of the pipeline without executing the endpoint: 'Test endpoint'. " +
            "Please register the EndpointMiddleware using 'IApplicationBuilder.UseEndpoints(...)' if " +
            "using routing.";
        Assert.Equal(expected, ex.Message);
        Assert.False(endpointCalled);
    }

    [Fact]
    public void BuildDoesNotCallMatchedEndpointWhenTerminated()
    {
        var builder = new ApplicationBuilder(null);
        builder.Run(context =>
        {
            // Do not call next
            return Task.CompletedTask;
        });
        var app = builder.Build();

        var endpointCalled = false;
        var endpoint = new Endpoint(
            context =>
            {
                endpointCalled = true;
                return Task.CompletedTask;
            },
            EndpointMetadataCollection.Empty,
            "Test endpoint");

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(endpoint);

        app.Invoke(httpContext);

        Assert.False(endpointCalled);
    }

    [Fact]
    public void PropertiesDictionaryIsDistinctAfterNew()
    {
        var builder1 = new ApplicationBuilder(null);
        builder1.Properties["test"] = "value1";

        var builder2 = builder1.New();
        builder2.Properties["test"] = "value2";

        Assert.Equal("value1", builder1.Properties["test"]);
    }

    private class TestHttpResponseFeature : IHttpResponseFeature
    {
        private int _statusCode = 200;
        public int StatusCode
        {
            get => _statusCode;
            set
            {
                _statusCode = HasStarted ? throw new NotSupportedException("The response has already started") : value;
            }
        }
        public string ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; }
        public Stream Body { get; set; } = Stream.Null;

        public bool HasStarted { get; set; }

        public void OnCompleted(Func<object, Task> callback, object state)
        {

        }

        public void OnStarting(Func<object, Task> callback, object state)
        {

        }
    }
}
