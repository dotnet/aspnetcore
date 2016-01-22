// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewFeatures.Internal
{
    public class ValidateAntiforgeryTokenAuthorizationFilterTest
    {
        [Theory]
        [InlineData("PUT")]
        [InlineData("POsT")]
        [InlineData("DeLETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("TracE")]
        [InlineData("OPTIONs")]
        public async Task Filter_ValidatesAntiforgery_ForAllMethods(string httpMethod)
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            antiforgery
                .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var filter = new ValidateAntiforgeryTokenAuthorizationFilter(antiforgery.Object, NullLoggerFactory.Instance);

            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            actionContext.HttpContext.Request.Method = httpMethod;

            var context = new AuthorizationContext(actionContext, new[] { filter });

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            antiforgery.Verify();
        }

        [Fact]
        public async Task Filter_SkipsAntiforgeryVerification_WhenOverridden()
        {
            // Arrange
            var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
            antiforgery
                .Setup(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()))
                .Returns(Task.FromResult(0))
                .Verifiable();

            var filter = new ValidateAntiforgeryTokenAuthorizationFilter(antiforgery.Object, NullLoggerFactory.Instance);

            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            actionContext.HttpContext.Request.Method = "POST";

            var context = new AuthorizationContext(actionContext, new IFilterMetadata[]
            {
                filter,
                new IgnoreAntiforgeryTokenAttribute(),
            });

            // Act
            await filter.OnAuthorizationAsync(context);

            // Assert
            antiforgery.Verify(a => a.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never());
        }
    }
}
