using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Moq;
using Xunit;

namespace Microsoft.AspNet.PipelineCore.Tests
{
    public class DefaultCanHasQueryTests
    {
        [Fact]
        public void QueryReturnsParsedQueryCollection()
        {
            // Arrange
            var features = new Mock<IFeatureCollection>();
            var request = new Mock<IHttpRequestInformation>();
            request.SetupGet(r => r.QueryString).Returns("foo=bar");

            object value = request.Object;
            features.Setup(f => f.TryGetValue(typeof(IHttpRequestInformation), out value))
                    .Returns(true);

            var provider = new DefaultCanHasQuery(features.Object);

            // Act
            var queryCollection = provider.Query;

            // Assert
            Assert.Equal("bar", queryCollection["foo"]);
        }
    }
}
