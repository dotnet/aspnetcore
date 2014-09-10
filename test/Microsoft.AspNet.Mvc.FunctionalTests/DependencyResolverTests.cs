#if ASPNET50
using System;
using System.Net.Http;
using System.Threading.Tasks;
using AutofacWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class DependencyResolverTests
    {
        [Theory]
        [InlineData("http://localhost/di", "<p>Builder Output: Hello from builder.</p>")]
        [InlineData("http://localhost/basic", "<p>Hello From Basic View</p>")]
        public async Task AutofacDIContainerCanUseMvc(string url, string expectedResponseBody)
        {
            // Arrange
            var provider = TestHelper.CreateServices("AutofacWebSite");
            Action<IApplicationBuilder> app = new Startup().Configure;
            HttpResponseMessage response = null;

            // Act & Assert
            await Assert.DoesNotThrowAsync(async () =>
            {
                // This essentially calls into the Startup.Configuration method
                var server = TestServer.Create(provider, app);

                // Make a request to start resolving DI pieces
                response = await server.CreateClient().GetAsync(url);
            });

            var actualResponseBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedResponseBody, actualResponseBody);
        }
    }
}
#endif
