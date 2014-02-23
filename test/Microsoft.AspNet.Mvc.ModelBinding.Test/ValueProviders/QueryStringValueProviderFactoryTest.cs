using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class QueryStringValueProviderFactoryTest
    {
        private readonly QueryStringValueProviderFactory _factory = new QueryStringValueProviderFactory();

        [Fact]
        public void GetValueProvider_WhenrequestContextParameterIsNull_Throws()
        {
            // Act and Assert
            ExceptionAssert.ThrowsArgumentNull(() => _factory.GetValueProvider(requestContext: null), "requestContext");
        }

        [Fact]
        public void GetValueProvider_ReturnsQueryStringValueProviderInstaceWithInvariantCulture()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            request.SetupGet(f => f.Query).Returns(Mock.Of<IReadableStringCollection>());
            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Items).Returns(new Dictionary<object, object>());
            context.SetupGet(c => c.Request).Returns(request.Object);
            var requestContext = new RequestContext(context.Object, new Dictionary<string, object>());

            // Act
            IValueProvider result = _factory.GetValueProvider(requestContext);

            // Assert
            var valueProvider = Assert.IsType<QueryStringValueProvider>(result);
            Assert.Equal(CultureInfo.InvariantCulture, valueProvider.Culture);
        }
    }
}
