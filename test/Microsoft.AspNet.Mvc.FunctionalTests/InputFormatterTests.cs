// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class InputFormatterTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("FormatterWebSite");
        private readonly Action<IApplicationBuilder> _app = new FormatterWebSite.Startup().Configure;

        [Fact]
        public async Task CheckIfXmlInputFormatterIsBeingCalled()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var sampleInputInt = 10;
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\"><SampleInt>"
                + sampleInputInt.ToString() + "</SampleInt></DummyClass>";
            var content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await client.PostAsync("http://localhost/Home/Index", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(sampleInputInt.ToString(), await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("application/*")]
        [InlineData("*/*")]
        [InlineData("text/json")]
        [InlineData("text/*")]
        public async Task JsonInputFormatter_IsSelectedForJsonRequest(string requestContentType)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var sampleInputInt = 10;
            var input = "{\"SampleInt\":10}";
            var content = new StringContent(input, Encoding.UTF8, requestContentType);

            // Act
            var response = await client.PostAsync("http://localhost/Home/Index", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(sampleInputInt.ToString(), await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("invalid", true)]
        [InlineData("application/custom", true)]
        [InlineData("image/jpg", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("invalid", false)]
        [InlineData("application/custom", false)]
        [InlineData("image/jpg", false)]
        public async Task ModelStateErrorValidation_NoInputFormatterFound_ForGivenContentType(string requestContentType,
                                                                                              bool filterHandlesModelStateError)
        {
            // Arrange
            var actionName = filterHandlesModelStateError ? "ActionFilterHandlesError" : "ActionHandlesError";
            var expectedSource = filterHandlesModelStateError ? "filter" : "action";

            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var input = "{\"SampleInt\":10}";
            var content = new StringContent(input);
            content.Headers.Clear();
            content.Headers.TryAddWithoutValidation("Content-Type", requestContentType);

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/InputFormatter/" + actionName);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            request.Content = content;
            var response = await client.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<FormatterWebSite.ErrorInfo>(responseBody);

            // Assert
            Assert.Equal(1, result.Errors.Count);
            Assert.Equal("Unsupported content type '" + requestContentType + "'.",
                         result.Errors[0]);
            Assert.Equal(actionName, result.ActionName);
            Assert.Equal("dummy", result.ParameterName);
            Assert.Equal(expectedSource, result.Source);
        }
    }
}