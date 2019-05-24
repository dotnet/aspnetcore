// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Builder.Internal
{
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
            builder.Use((context, next) =>
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
    }
}
