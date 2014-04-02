using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class UrlHelperTest
    {
        [Theory]
        [InlineData("", "/Home/About", "/Home/About")]
        [InlineData("/myapproot", "/test", "/test")]
        public void Content_ReturnsContentPath_WhenItDoesNotStartWithToken(string appRoot, 
                                                                           string contentPath, 
                                                                           string expectedPath)
        {
            // Arrange
            var context = CreateHttpContext(appRoot);
            var contextAccessor = CreateActionContext(context);
            var urlHelper = new UrlHelper(contextAccessor);

            // Act
            var path = urlHelper.Content(contentPath);

            // Assert
            Assert.Equal(expectedPath, path);
        }

        [Theory]
        [InlineData(null, "~/Home/About", "/Home/About")]
        [InlineData("/", "~/Home/About", "/Home/About")]
        [InlineData("/", "~/", "/")]
        [InlineData("", "~/Home/About", "/Home/About")]
        [InlineData("/myapproot", "~/", "/myapproot/")]
        [InlineData("", "~/Home/About", "/Home/About")]
        [InlineData("/myapproot", "~/", "/myapproot/")]
        public void Content_ReturnsAppRelativePath_WhenItStartsWithToken(string appRoot, 
                                                                         string contentPath, 
                                                                         string expectedPath)
        {
            // Arrange
            var context = CreateHttpContext(appRoot);
            var contextAccessor = CreateActionContext(context);
            var urlHelper = new UrlHelper(contextAccessor);

            // Act
            var path = urlHelper.Content(contentPath);

            // Assert
            Assert.Equal(expectedPath, path);
        }

        private static HttpContext CreateHttpContext(string appRoot)
        {
            var appRootPath = new PathString(appRoot);
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.PathBase)
                   .Returns(appRootPath);
            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Request)
                   .Returns(request.Object);
            return context.Object;
        }

        private static IContextAccessor<ActionContext> CreateActionContext(HttpContext context)
        {
            var actionContext = new ActionContext(context,
                                                  Mock.Of<IRouter>(),
                                                  new Dictionary<string, object>(),
                                                  new ActionDescriptor());
            var contextAccessor = new Mock<IContextAccessor<ActionContext>>();
            contextAccessor.SetupGet(c => c.Value)
                           .Returns(actionContext);
            return contextAccessor.Object;
        }
    }
}
