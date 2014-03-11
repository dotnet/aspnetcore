using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.PipelineCore.Tests
{
    public class DefaultHttpRequestTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(9001)]
        [InlineData(65535)]
        public void GetContentLength_ReturnsParsedHeader(long value)
        {
            // Arrange
            var request = GetRequest(value.ToString(CultureInfo.InvariantCulture));

            // Act and Assert
            Assert.Equal(value, request.ContentLength);
        }

        [Fact]
        public void GetContentLength_ReturnsNullIfHeaderDoesNotExist()
        {
            // Arrange
            var request = GetRequest(contentLength: null);

            // Act and Assert
            Assert.Null(request.ContentLength);
        }

        [Theory]
        [InlineData("cant-parse-this")]
        [InlineData("-1000")]
        [InlineData("1000.00")]
        [InlineData("100/5")]
        public void GetContentLength_ReturnsNullIfHeaderCannotBeParsed(string contentLength)
        {
            // Arrange
            var request = GetRequest(contentLength);

            // Act and Assert
            Assert.Null(request.ContentLength);
        }

        private static DefaultHttpRequest GetRequest(string contentLength = null)
        {
            var features = new Mock<IFeatureCollection>();
            var mockRequestInfo = new Mock<IHttpRequestInformation>();
            var headers = new Dictionary<string, string[]>();
            if (contentLength != null)
            {
                headers.Add("Content-Length", new[] { contentLength });
                
            }
            mockRequestInfo.SetupGet(r => r.Headers)
                           .Returns(headers);
            object requestInfo = mockRequestInfo.Object;
            features.Setup(f => f.TryGetValue(typeof(IHttpRequestInformation), out requestInfo))
                    .Returns(true);
            var context = new DefaultHttpContext(features.Object);
            var request = new DefaultHttpRequest(context, features.Object);
            return request;
        }
    }
}
