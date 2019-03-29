// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits
{
    public class CircuitPrerendererTest
    {
        // Because CircuitPrerenderer is a point of integration with HttpContext,
        // it's not a good candidate for unit testing. The majority of prerendering
        // unit tests should be elsewhere in HtmlRendererTests inside the
        // Microsoft.AspNetCore.Components.Tests projects.
        //
        // The only unit tests added here should specifically be about how we're
        // interacting with the HttpContext for configuring the prerenderer.

        [Fact]
        public async Task ExtractsUriFromHttpContext_EmptyPathBase()
        {
            // Arrange
            var circuitFactory = new TestCircuitFactory();
            var circuitPrerenderer = new CircuitPrerenderer(circuitFactory);
            var httpContext = new Mock<HttpContext>();
            var httpRequest = new Mock<HttpRequest>().SetupAllProperties();
            httpContext.Setup(h => h.Request).Returns(httpRequest.Object);
            httpRequest.Object.Scheme = "https";
            httpRequest.Object.Host = new HostString("example.com", 1234);
            httpRequest.Object.Path = "/some/path";

            var prerenderingContext = new ComponentPrerenderingContext
            {
                ComponentType = typeof(UriDisplayComponent),
                Parameters = ParameterCollection.Empty,
                Context = httpContext.Object
            };

            // Act
            var result = await circuitPrerenderer.PrerenderComponentAsync(prerenderingContext);

            // Assert
            Assert.Equal(new[]
            {
                "The current URI is ",
                "https://example.com:1234/some/path",
                " within base URI ",
                "https://example.com:1234/"
            }, result);
        }

        [Fact]
        public async Task ExtractsUriFromHttpContext_NonemptyPathBase()
        {
            // Arrange
            var circuitFactory = new TestCircuitFactory();
            var circuitPrerenderer = new CircuitPrerenderer(circuitFactory);
            var httpContext = new Mock<HttpContext>();
            var httpRequest = new Mock<HttpRequest>().SetupAllProperties();
            httpContext.Setup(h => h.Request).Returns(httpRequest.Object);
            httpRequest.Object.Scheme = "https";
            httpRequest.Object.Host = new HostString("example.com", 1234);
            httpRequest.Object.PathBase = "/my/dir";
            httpRequest.Object.Path = "/some/path";

            var prerenderingContext = new ComponentPrerenderingContext
            {
                ComponentType = typeof(UriDisplayComponent),
                Parameters = ParameterCollection.Empty,
                Context = httpContext.Object
            };

            // Act
            var result = await circuitPrerenderer.PrerenderComponentAsync(prerenderingContext);

            // Assert
            Assert.Equal(new[]
            {
                "The current URI is ",
                "https://example.com:1234/my/dir/some/path",
                " within base URI ",
                "https://example.com:1234/my/dir/"
            }, result);
        }

        class TestCircuitFactory : CircuitFactory
        {
            public override CircuitHost CreateCircuitHost(HttpContext httpContext, CircuitClientProxy client, string uriAbsolute, string baseUriAbsolute)
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddScoped<IUriHelper>(_ =>
                {
                    var uriHelper = new RemoteUriHelper();
                    uriHelper.Initialize(uriAbsolute, baseUriAbsolute);
                    return uriHelper;
                });
                var serviceScope = serviceCollection.BuildServiceProvider().CreateScope();
                return TestCircuitHost.Create(serviceScope);
            }
        }

        class UriDisplayComponent : IComponent
        {
            private RenderHandle _renderHandle;

            [Inject] IUriHelper UriHelper { get; set; }

            public void Configure(RenderHandle renderHandle)
            {
                _renderHandle = renderHandle;
            }

            public Task SetParametersAsync(ParameterCollection parameters)
            {
                _renderHandle.Render(builder =>
                {
                    builder.AddContent(0, "The current URI is ");
                    builder.AddContent(1, UriHelper.GetAbsoluteUri());
                    builder.AddContent(2, " within base URI ");
                    builder.AddContent(3, UriHelper.GetBaseUri());
                });

                return Task.CompletedTask;
            }
        }
    }
}
