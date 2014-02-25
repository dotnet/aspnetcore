using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Moq;
using Xunit;

namespace Microsoft.AspNet.PipelineCore.Tests
{
    public class DefaultCanHasFormTests
    {
        [Fact]
        public async Task GetFormAsync_ReturnsParsedFormCollection()
        {
            // Arrange
            var formContent = Encoding.UTF8.GetBytes("foo=bar&baz=2");
            var features = new Mock<IFeatureCollection>();
            var request = new Mock<IHttpRequestInformation>();
            request.SetupGet(r => r.Body).Returns(new MemoryStream(formContent));

            object value = request.Object;
            features.Setup(f => f.TryGetValue(typeof(IHttpRequestInformation), out value))
                    .Returns(true);

            var provider = new DefaultCanHasForm(features.Object);

            // Act
            var formCollection = await provider.GetFormAsync();

            // Assert
            Assert.Equal("bar", formCollection["foo"]);
            Assert.Equal("2", formCollection["baz"]);
        }

        [Fact]
        public async Task GetFormAsync_CachesFormCollectionPerBodyStream()
        {
            // Arrange
            var formContent1 = Encoding.UTF8.GetBytes("foo=bar&baz=2");
            var formContent2 = Encoding.UTF8.GetBytes("collection2=value");
            var features = new Mock<IFeatureCollection>();
            var request = new Mock<IHttpRequestInformation>();
            request.SetupGet(r => r.Body).Returns(new MemoryStream(formContent1));

            object value = request.Object;
            features.Setup(f => f.TryGetValue(typeof(IHttpRequestInformation), out value))
                    .Returns(true);

            var provider = new DefaultCanHasForm(features.Object);

            // Act - 1
            var formCollection = await provider.GetFormAsync();

            // Assert - 1
            Assert.Equal("bar", formCollection["foo"]);
            Assert.Equal("2", formCollection["baz"]);
            Assert.Same(formCollection, await provider.GetFormAsync());

            // Act - 2
            request.SetupGet(r => r.Body).Returns(new MemoryStream(formContent2));
            formCollection = await provider.GetFormAsync();

            // Assert - 2
            Assert.Equal("value", formCollection["collection2"]);
        }
    }
}
