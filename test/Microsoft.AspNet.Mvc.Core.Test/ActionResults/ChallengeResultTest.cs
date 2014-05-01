using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test.ActionResults
{
    public class ChallengeResultTest
    {
        [Fact]
        public void ChallengeResult_Execute()
        {
            // Arrange
            var result = new ChallengeResult(new string[] { }, null);
            var httpContext = new Mock<HttpContext>();
            var httpResponse = new Mock<HttpResponse>();
            httpContext.Setup(o => o.Response).Returns(httpResponse.Object);
            var actionContext = new ActionContext(httpContext.Object,
                                                  Mock.Of<IRouter>(),
                                                  new Dictionary<string, object>(),
                                                  new ActionDescriptor());

            // Act
            result.ExecuteResult(actionContext);

            // Assert
            httpResponse.Verify(c => c.Challenge(new string[] { }, null), Times.Exactly(1));
        }
    }
}