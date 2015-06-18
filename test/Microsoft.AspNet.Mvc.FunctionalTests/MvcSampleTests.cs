// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Xml;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class MvcSampleTests
    {
        private const string SiteName = nameof(MvcSample) + "." + nameof(MvcSample.Web);

        // Path relative to Mvc\\test\Microsoft.AspNet.Mvc.FunctionalTests
        private readonly static string SamplesFolder = Path.Combine("..", "..", "samples");

        private readonly Action<IApplicationBuilder> _app = new MvcSample.Web.Startup().Configure;
        private readonly Func<IServiceCollection, IServiceProvider> _configureServices = new MvcSample.Web.Startup().ConfigureServices;

        [Theory]
        [InlineData("")]                        // Shared/MyView.cshtml
        [InlineData("/")]                       // Shared/MyView.cshtml
        [InlineData("/Home/Index")]             // Shared/MyView.cshtml
        [InlineData("/Home/Create")]            // Home/Create.cshtml
        [InlineData("/Home/FlushPoint")]        // Home/FlushPoint.cshtml
        [InlineData("/Home/InjectSample")]      // Home/InjectSample.cshtml
        [InlineData("/Home/Language")]          // Home/Language.cshtml
        [InlineData("/Home/MyView")]            // Shared/MyView.cshtml
        [InlineData("/Home/NullUser")]          // Home/NullUser.cshtml
        [InlineData("/Home/Post")]              // Shared/MyView.cshtml with null User
        [InlineData("/Home/SaveUser")]          // Shared/MyView.cshtml with uninitialized User instance
        [InlineData("/Home/ValidationSummary")] // Home/ValidationSummary.cshtml
        public async Task Home_Pages_ReturnSuccess(string path)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, SamplesFolder, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost" + path);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("Name=SamplePerson&Address.Street=SampleStreet&Address.City=SampleCity&" +
                    "Address.State=SampleState&Address.ZipCode=11&PastJobs[0].JobTitle=SampleJob1&" +
                    "PastJobs[0].EmployerName=Employer1&PastJobs[0].Years=2&PastJobs[1].JobTitle=SampleJob2&" +
                    "PastJobs[1].EmployerName=Employer2&PastJobs[1].Years=4&PastJobs[2].JobTitle=SampleJob3&" +
                    "PastJobs[2].EmployerName=Employer3&PastJobs[2].Years=1", "true")]
        // Input with some special characters
        [InlineData("Name=SamplePerson&Address.Street=SampleStre'et&Address.City=S\ampleCity&" +
                    "Address.State=SampleState&Address.ZipCode=11&PastJobs[0].JobTitle=S~ampleJob1&" +
                    "PastJobs[0].EmployerName=Employer1&PastJobs[0].Years=2&PastJobs[1].JobTitle=SampleJob2&" +
                    "PastJobs[1].EmployerName=Employer2&PastJobs[1].Years=4&PastJobs[2].JobTitle=SampleJob3&" +
                    "PastJobs[2].EmployerName=Employer3&PastJobs[2].Years=1", "true")]
        [InlineData("Name=SamplePerson", "false")]
        public async Task FormUrlEncoded_ReturnsAppropriateResults(string input, string expectedOutput)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, SamplesFolder, _configureServices);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/FormUrlEncoded/IsValidPerson");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(input, Encoding.UTF8, "application/x-www-form-urlencoded");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(expectedOutput, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormUrlEncoded_Index_ReturnSuccess()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, SamplesFolder, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormUrlEncoded");

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Home_NotFoundAction_Returns404()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, SamplesFolder, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/NotFound");

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [ConditionalTheory]
        // Mono.Xml2.XmlTextReader.ReadText is unable to read the XML. This is fixed in mono 4.3.0.
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Home_CreateUser_ReturnsXmlBasedOnAcceptHeader()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, SamplesFolder, _configureServices);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Home/ReturnUser");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml;charset=utf-8"));

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal("<User xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=" +
            "\"http://schemas.datacontract.org/2004/07/MvcSample.Web.Models\"><About>I like playing Football" +
            "</About><Address>My address</Address><Age>13</Age><Alive>true</Alive><Dependent><About i:nil=\"true\" />" +
            "<Address>Dependents address</Address><Age>0</Age><Alive>false</Alive><Dependent i:nil=\"true\" />" +
            "<GPA>0</GPA><Log i:nil=\"true\" /><Name>Dependents name</Name>" +
            "<Profession i:nil=\"true\" /></Dependent><GPA>13.37</GPA><Log i:nil=\"true\" />" +
            "<Name>My name</Name><Profession>Software Engineer</Profession></User>",
                await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("http://localhost/Filters/ChallengeUser", HttpStatusCode.Unauthorized)]
        [InlineData("http://localhost/Filters/AllGranted", HttpStatusCode.Unauthorized)]
        [InlineData("http://localhost/Filters/NotGrantedClaim", HttpStatusCode.Unauthorized)]
        public async Task FiltersController_Tests(string url, HttpStatusCode statusCode)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, SamplesFolder, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(statusCode, response.StatusCode);
        }

        [Fact]
        public async Task FiltersController_Crash_ThrowsException()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, SamplesFolder, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Filters/Crash?message=HelloWorld");

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Boom HelloWorld", await response.Content.ReadAsStringAsync());
        }
    }
}