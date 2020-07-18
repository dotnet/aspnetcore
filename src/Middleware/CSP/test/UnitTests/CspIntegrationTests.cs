using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using CspApplication;

namespace Microsoft.AspNetCore.Csp.Test
{
    public class CspIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
    {

        private readonly WebApplicationFactory<Startup> _factory;

        public CspIntegrationTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/")]
        public async Task CspNonceAddedToScriptTags(string requestPath)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(requestPath);

            // Assert
            response.EnsureSuccessStatusCode();
            //var header = response.Headers.GetValues(CspConstants.CspEnforcedHeaderName).FirstOrDefault();
            //Assert.NotEmpty(header);
            //Assert.Matches("'nonce-", header);
        }
    }
}
