// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Builder
{
    // These are really more like integration tests. They verify that these extensions
    // add routes that behave as advertised.
    public class RequestDelegateRouteBuilderExtensionsTest
    {
        private static readonly RequestDelegate NullHandler = (c) => Task.FromResult(0);

        public static TheoryData<Action<IRouteBuilder>, Action<HttpContext>> MatchingActions
        {
            get
            {
                return new TheoryData<Action<IRouteBuilder>, Action<HttpContext>>()
                {
                    { b => { b.MapRoute("api/{id}", NullHandler); }, null },
                    { b => { b.MapRoute("api/{id}", app => { }); }, null },

                    { b => { b.MapDelete("api/{id}", NullHandler); }, c => { c.Request.Method = "DELETE"; } },
                    { b => { b.MapDelete("api/{id}", app => { }); }, c => { c.Request.Method = "DELETE"; }  },
                    { b => { b.MapGet("api/{id}", NullHandler); }, c => { c.Request.Method = "GET"; }  },
                    { b => { b.MapGet("api/{id}", app => { }); }, c => { c.Request.Method = "GET"; }  },
                    { b => { b.MapPost("api/{id}", NullHandler); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapPost("api/{id}", app => { }); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapPut("api/{id}", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapPut("api/{id}", app => { }); }, c => { c.Request.Method = "PUT"; }  },

                    { b => { b.MapVerb("PUT", "api/{id}", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapVerb("PUT", "api/{id}", app => { }); }, c => { c.Request.Method = "PUT"; }  },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingActions))]
        public async Task Map_MatchesRequest(
            Action<IRouteBuilder> routeSetup,
            Action<HttpContext> requestSetup)
        {
            // Arrange
            var services = CreateServices();

            var context = CreateRouteContext(services);
            context.HttpContext.Request.Path = new PathString("/api/5");
            requestSetup?.Invoke(context.HttpContext);

            var builder = CreateRouteBuilder(services);
            routeSetup(builder);
            var route = builder.Build();

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Same(NullHandler, context.Handler);
        }

        public static TheoryData<Action<IRouteBuilder>, Action<HttpContext>> NonmatchingActions
        {
            get
            {
                return new TheoryData<Action<IRouteBuilder>, Action<HttpContext>>()
                {
                    { b => { b.MapRoute("api/{id}/extra", NullHandler); }, null },
                    { b => { b.MapRoute("api/{id}/extra", app => { }); }, null },

                    { b => { b.MapDelete("api/{id}", NullHandler); }, c => { c.Request.Method = "GET"; } },
                    { b => { b.MapDelete("api/{id}", app => { }); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapDelete("api/{id}/extra", NullHandler); }, c => { c.Request.Method = "DELETE"; } },
                    { b => { b.MapDelete("api/{id}/extra", app => { }); }, c => { c.Request.Method = "DELETE"; }  },
                    { b => { b.MapGet("api/{id}", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapGet("api/{id}", app => { }); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapGet("api/{id}/extra", NullHandler); }, c => { c.Request.Method = "GET"; }  },
                    { b => { b.MapGet("api/{id}/extra", app => { }); }, c => { c.Request.Method = "GET"; }  },
                    { b => { b.MapPost("api/{id}", NullHandler); }, c => { c.Request.Method = "MEH"; }  },
                    { b => { b.MapPost("api/{id}", app => { }); }, c => { c.Request.Method = "DELETE"; }  },
                    { b => { b.MapPost("api/{id}/extra", NullHandler); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapPost("api/{id}/extra", app => { }); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapPut("api/{id}", NullHandler); }, c => { c.Request.Method = "BLEH"; }  },
                    { b => { b.MapPut("api/{id}", app => { }); }, c => { c.Request.Method = "HEAD"; }  },
                    { b => { b.MapPut("api/{id}/extra", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapPut("api/{id}/extra", app => { }); }, c => { c.Request.Method = "PUT"; }  },

                    { b => { b.MapVerb("PUT", "api/{id}", NullHandler); }, c => { c.Request.Method = "POST"; }  },
                    { b => { b.MapVerb("PUT", "api/{id}", app => { }); }, c => { c.Request.Method = "HEAD"; }  },
                    { b => { b.MapVerb("PUT", "api/{id}/extra", NullHandler); }, c => { c.Request.Method = "PUT"; }  },
                    { b => { b.MapVerb("PUT", "api/{id}/extra", app => { }); }, c => { c.Request.Method = "PUT"; }  },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NonmatchingActions))]
        public async Task Map_DoesNotMatchRequest(
            Action<IRouteBuilder> routeSetup,
            Action<HttpContext> requestSetup)
        {
            // Arrange
            var services = CreateServices();

            var context = CreateRouteContext(services);
            context.HttpContext.Request.Path = new PathString("/api/5");
            requestSetup?.Invoke(context.HttpContext);

            var builder = CreateRouteBuilder(services);
            routeSetup(builder);
            var route = builder.Build();

            // Act
            await route.RouteAsync(context);

            // Assert
            Assert.Null(context.Handler);
        }

        private static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddRouting();
            services.AddLogging();
            return services.BuildServiceProvider();
        }

        private static RouteContext CreateRouteContext(IServiceProvider services)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services;
            return new RouteContext(httpContext);
        }

        private static IRouteBuilder CreateRouteBuilder(IServiceProvider services)
        {
            var applicationBuilder = new Mock<IApplicationBuilder>();
            applicationBuilder.SetupAllProperties();

            applicationBuilder
                .Setup(b => b.New().Build())
                .Returns(NullHandler);

            applicationBuilder.Object.ApplicationServices = services;

            var routeBuilder = new RouteBuilder(applicationBuilder.Object);
            return routeBuilder;
        }
    }
}
