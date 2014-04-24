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
            var urlHelper = CreateUrlHelper(contextAccessor);

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
            var urlHelper = CreateUrlHelper(contextAccessor);

            // Act
            var path = urlHelper.Content(contentPath);

            // Assert
            Assert.Equal(expectedPath, path);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsLocalUrl_ReturnsFalseOnEmpty(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("/foo.html")]
        [InlineData("/www.example.com")]
        [InlineData("/")]
        public void IsLocalUrl_AcceptsRootedUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("~/")]
        [InlineData("~/foo.html")]
        public void IsLocalUrl_AcceptsApplicationRelativeUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("foo.html")]
        [InlineData("../foo.html")]
        [InlineData("fold/foo.html")]
        public void IsLocalUrl_RejectsRelativeUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http:/foo.html")]
        [InlineData("hTtP:foo.html")]
        [InlineData("http:/www.example.com")]
        [InlineData("HtTpS:/www.example.com")]
        public void IsLocalUrl_RejectValidButUnsafeRelativeUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper();

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http://www.mysite.com/appDir/foo.html")]
        [InlineData("http://WWW.MYSITE.COM")]
        public void IsLocalUrl_RejectsUrlsOnTheSameHost(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http://localhost/foobar.html")]
        [InlineData("http://127.0.0.1/foobar.html")]
        public void IsLocalUrl_RejectsUrlsOnLocalHost(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("https://www.mysite.com/")]
        public void IsLocalUrl_RejectsUrlsOnTheSameHostButDifferentScheme(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http://www.example.com")]
        [InlineData("https://www.example.com")]
        [InlineData("hTtP://www.example.com")]
        [InlineData("HtTpS://www.example.com")]
        public void IsLocalUrl_RejectsUrlsOnDifferentHost(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http://///www.example.com/foo.html")]
        [InlineData("https://///www.example.com/foo.html")]
        [InlineData("HtTpS://///www.example.com/foo.html")]
        [InlineData("http:///www.example.com/foo.html")]
        [InlineData("http:////www.example.com/foo.html")]
        [InlineData("http://///www.example.com/foo.html")]
        public void IsLocalUrl_RejectsUrlsWithTooManySchemeSeparatorCharacters(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("//www.example.com")]
        [InlineData("//www.example.com?")]
        [InlineData("//www.example.com:80")]
        [InlineData("//www.example.com/foobar.html")]
        [InlineData("///www.example.com")]
        [InlineData("//////www.example.com")]
        public void IsLocalUrl_RejectsUrlsWithMissingSchemeName(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("http:\\\\www.example.com")]
        [InlineData("http:\\\\www.example.com\\")]
        [InlineData("/\\")]
        [InlineData("/\\foo")]
        public void IsLocalUrl_RejectsInvalidUrls(string url)
        {
            // Arrange
            var helper = CreateUrlHelper("www.mysite.com");

            // Act
            var result = helper.IsLocalUrl(url);

            // Assert
            Assert.False(result);
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

        private static UrlHelper CreateUrlHelper()
        {
            var context = CreateHttpContext(string.Empty);
            var actionContext = CreateActionContext(context);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(string host)
        {
            var context = CreateHttpContext(string.Empty);
            context.Request.Host = new HostString(host);

            var actionContext = CreateActionContext(context);

            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(actionContext, actionSelector.Object);
        }

        private static UrlHelper CreateUrlHelper(IContextAccessor<ActionContext> contextAccessor)
        {
            var actionSelector = new Mock<IActionSelector>(MockBehavior.Strict);
            return new UrlHelper(contextAccessor, actionSelector.Object);
        }
    }
}
