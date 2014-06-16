using System;
using System.IO;
using System.Threading.Tasks;
using AutofacWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
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
            Action<IBuilder> app = new Startup().Configure;
            TestServer server = null;
            HttpResponse response = null;

            // Act & Assert
            await Assert.DoesNotThrowAsync(async () =>
            {
                // This essentially calls into the Startup.Configuration method
                server = TestServer.Create(provider, app);

                // Make a request to start resolving DI pieces
                response = await server.Handler.GetAsync(url);
            });

            var actualResponseBody = new StreamReader(response.Body).ReadToEnd();
            Assert.Equal(expectedResponseBody, actualResponseBody);
        }
    }
}