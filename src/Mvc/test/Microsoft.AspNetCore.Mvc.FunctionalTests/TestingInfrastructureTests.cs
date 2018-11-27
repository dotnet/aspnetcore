using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using BasicWebSite;
using BasicWebSite.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TestingInfrastructureTests : IClassFixture<WebApplicationFactory<BasicWebSite.Startup>>
    {
        public TestingInfrastructureTests(WebApplicationFactory<BasicWebSite.Startup> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.ConfigureTestServices(s => s.AddSingleton<TestService, OverridenService>());

        public HttpClient Client { get; }

        [Fact]
        public async Task TestingInfrastructure_CanOverrideServiceFromWithinTheTest()
        {
            // Act
            var response = await Client.GetStringAsync("Testing/Builder");

            // Assert
            Assert.Equal("Test", response);
        }

        [Fact]
        public async Task TestingInfrastructure_RedirectHandlerWorksWithPreserveMethod()
        {
            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "Testing/RedirectHandler/2")
            {
                Content = new ObjectContent<Number>(new Number { Value = 5 }, new JsonMediaTypeFormatter())
            };
            request.Headers.Add("X-Pass-Thru", "Some-Value");
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var xPassThruValue = Assert.Single(response.Headers.GetValues("X-Pass-Thru"));
            Assert.Equal("Some-Value", xPassThruValue);

            var handlerResponse = await response.Content.ReadAsAsync<RedirectHandlerResponse>();
            Assert.Equal(5, handlerResponse.Url);
            Assert.Equal(5, handlerResponse.Body);
        }

        [Fact]
        public async Task TestingInfrastructure_PostRedirectGetWorksWithCookies()
        {
            // Act
            var acquireToken = await Client.GetAsync("Testing/AntiforgerySimulator/3");
            Assert.Equal(HttpStatusCode.OK, acquireToken.StatusCode);

            var response = await Client.PostAsync(
                "Testing/PostRedirectGet/Post/3",
                content: null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var handlerResponse = await response.Content.ReadAsAsync<PostRedirectGetGetResponse>();
            Assert.Equal(4, handlerResponse.TempDataValue);
            Assert.Equal("Value-4", handlerResponse.CookieValue);
        }

        [Fact]
        public async Task TestingInfrastructure_PutWithoutBodyFollowsRedirects()
        {
            // Act
            var response = await Client.PutAsync("Testing/Put/3", content: null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(5, await response.Content.ReadAsAsync<int>());
        }

        private class OverridenService : TestService
        {
            public OverridenService()
            {
                Message = "Test";
            }
        }
    }
}
