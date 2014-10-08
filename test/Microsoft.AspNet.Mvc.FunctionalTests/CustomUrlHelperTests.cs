using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    /// <summary>
    /// The tests here verify the extensibility of <see cref="UrlHelper"/>.
    /// 
    /// Following are some of the scenarios exercised here:
    /// 1. Based on configuration, generate Content urls pointing to local or a CDN server
    /// 2. Based on configuration, generate lower case urls
    /// </summary>
    public class CustomUrlHelperTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("UrlHelperWebSite");
        private readonly Action<IApplicationBuilder> _app = new UrlHelperWebSite.Startup().Configure;
        private const string _cdnServerBaseUrl = "http://cdn.contoso.com";

        [Fact]
        public async Task CustomUrlHelper_GeneratesUrlFromController()
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();

                // Act
                var response = await client.GetAsync("http://localhost/Home/UrlContent");
                var responseData = await response.Content.ReadAsStringAsync();

                //Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(_cdnServerBaseUrl + "/bootstrap.min.css", responseData);
            }
        }

        [Fact]
        public async Task CustomUrlHelper_GeneratesUrlFromView()
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();

                // Act
                var response = await client.GetAsync("http://localhost/Home/Index");
                var responseData = await response.Content.ReadAsStringAsync();

                //Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(_cdnServerBaseUrl + "/bootstrap.min.css", responseData);
            }
        }

        [Theory]
        [InlineData("http://localhost/Home/LinkByUrlRouteUrl", "/api/simplepoco/10")]
        [InlineData("http://localhost/Home/LinkByUrlAction", "/home/urlcontent")]
        public async Task LowercaseUrls_LinkGeneration(string url, string expectedLink)
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();

                // Act
                var response = await client.GetAsync(url);
                var responseData = await response.Content.ReadAsStringAsync();

                //Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(expectedLink, responseData, ignoreCase: false);
            }
        }
    }
}