using Microsoft.AspNet.Abstractions;
using Xunit;

namespace Microsoft.AspNet.PipelineCore.Tests
{
    public class BuilderTests
    {
        [Fact]
        public void BuildReturnsCallableDelegate()
        {
            var builder = new Builder();
            var app = builder.Build();

            var mockHttpContext = new Moq.Mock<HttpContext>();
            var mockHttpResponse = new Moq.Mock<HttpResponse>();
            mockHttpContext.SetupGet(x => x.Response).Returns(mockHttpResponse.Object);
            mockHttpResponse.SetupProperty(x => x.StatusCode);

            app.Invoke(mockHttpContext.Object);
            Assert.Equal(mockHttpContext.Object.Response.StatusCode, 404);
        }
    }
}