// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using BasicWebSite.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class ComponentRenderingFunctionalTests : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public ComponentRenderingFunctionalTests(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = Client ?? CreateClient(fixture);
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task Renders_BasicComponent()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/components");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();

            AssertComponent("\n    <p>Hello world!</p>\n", "Greetings", content);
        }

        [Fact]
        public async Task Renders_AsyncComponent()
        {
            // Arrange & Act
            var expectedHtml = @"
    <h1>Weather forecast</h1>

<p>This component demonstrates fetching data from the server.</p>

    <p>Weather data for 01/15/2019</p>
    <table class=""table"">
        <thead>
            <tr>
                <th>Date</th>
                <th>Temp. (C)</th>
                <th>Temp. (F)</th>
                <th>Summary</th>
            </tr>
        </thead>
        <tbody>
                <tr>
                    <td>06/05/2018</td>
                    <td>1</td>
                    <td>33</td>
                    <td>Freezing</td>
                </tr>
                <tr>
                    <td>07/05/2018</td>
                    <td>14</td>
                    <td>57</td>
                    <td>Bracing</td>
                </tr>
                <tr>
                    <td>08/05/2018</td>
                    <td>-13</td>
                    <td>9</td>
                    <td>Freezing</td>
                </tr>
                <tr>
                    <td>09/05/2018</td>
                    <td>-16</td>
                    <td>4</td>
                    <td>Balmy</td>
                </tr>
                <tr>
                    <td>10/05/2018</td>
                    <td>2</td>
                    <td>29</td>
                    <td>Chilly</td>
                </tr>
        </tbody>
    </table>

";

            var response = await Client.GetAsync("http://localhost/components");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();

            AssertComponent(expectedHtml, "FetchData", content);
        }

        private void AssertComponent(string expectedConent, string divId, string responseContent)
        {
            var parser = new HtmlParser();
            var htmlDocument = parser.Parse(responseContent);
            var div = htmlDocument.Body.QuerySelector($"#{divId}");
            Assert.Equal(
                expectedConent.Replace("\r\n","\n"),
                div.InnerHtml.Replace("\r\n","\n"));
        }

        // A simple delegating handler used in setting up test services so that we can configure
        // services that talk back to the TestServer using HttpClient.
        private class LoopHttpHandler : DelegatingHandler
        {
        }

        private HttpClient CreateClient(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            var loopHandler = new LoopHttpHandler();

            var client = fixture
                .WithWebHostBuilder(builder => builder.ConfigureServices(ConfigureTestWeatherForecastService))
                .CreateClient();

            // We configure the inner handler with a handler to this TestServer instance so that calls to the
            // server can get routed properly.
            loopHandler.InnerHandler = fixture.Server.CreateHandler();

            void ConfigureTestWeatherForecastService(IServiceCollection services) =>
                // We configure the test service here with an HttpClient that uses this loopback handler to talk
                // to this TestServer instance.
                services.AddSingleton(new WeatherForecastService(new HttpClient(loopHandler)
                {
                    BaseAddress = fixture.ClientOptions.BaseAddress
                }));

            return client;
        }
    }
}
