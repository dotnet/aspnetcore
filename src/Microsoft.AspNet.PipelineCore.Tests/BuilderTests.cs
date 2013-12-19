using Microsoft.AspNet.Abstractions;
using Shouldly;
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

            var mockHttpContext = new Moq.Mock<HttpContextBase>();
            var mockHttpResponse = new Moq.Mock<HttpResponseBase>();
            mockHttpContext.SetupGet(x => x.Response).Returns(mockHttpResponse.Object);
            mockHttpResponse.SetupProperty(x => x.StatusCode);

            app.Invoke(mockHttpContext.Object);
            mockHttpContext.Object.Response.StatusCode.ShouldBe(404);
        }
    }
}
