using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpStatusCodeResultTests
    {
        [Fact]
        public void HttpStatusCodeResult_ExecuteResultSetsResponseStatusCode()
        {
            // Arrange
            var result = new HttpStatusCodeResult(404);

            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();

            var context = new ActionContext(httpContext, routeData, actionDescriptor);

            // Act
            result.ExecuteResult(context);

            // Assert
            Assert.Equal(404, httpContext.Response.StatusCode);
        }
    }
}