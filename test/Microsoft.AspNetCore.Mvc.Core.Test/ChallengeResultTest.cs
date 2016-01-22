// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class ChallengeResultTest
    {
        [Fact]
        public async Task ChallengeResult_Execute()
        {
            // Arrange
            var result = new ChallengeResult("", null);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices).Returns(CreateServices().BuildServiceProvider());

            var auth = new Mock<AuthenticationManager>();
            httpContext.Setup(o => o.Authentication).Returns(auth.Object);

            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            var actionContext = new ActionContext(httpContext.Object,
                                                  routeData,
                                                  new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            auth.Verify(c => c.ChallengeAsync("", null), Times.Exactly(1));
        }

        [Fact]
        public async Task ChallengeResult_ExecuteNoSchemes()
        {
            // Arrange
            var result = new ChallengeResult(new string[] { }, null);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.RequestServices).Returns(CreateServices().BuildServiceProvider());

            var auth = new Mock<AuthenticationManager>();
            httpContext.Setup(o => o.Authentication).Returns(auth.Object);

            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            var actionContext = new ActionContext(httpContext.Object,
                                                  routeData,
                                                  new ActionDescriptor());

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            auth.Verify(c => c.ChallengeAsync((AuthenticationProperties)null), Times.Exactly(1));
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services;
        }
    }
}