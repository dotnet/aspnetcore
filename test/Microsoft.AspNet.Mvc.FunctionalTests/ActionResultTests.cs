// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ActionResultsWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ActionResultTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("ActionResultsWebSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private const string sampleIntError = "The field SampleInt must be between 10 and 100.";
        private const string sampleStringError =
            "The field SampleString must be a string or array type with a minimum length of '15'.";
                
        [Fact]
        public async Task BadRequestResult_CanBeReturned()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var input = "{\"SampleInt\":10}";

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetBadResult");

            request.Content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsRelativePathInLocationHeader()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedRelative");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsAbsolutePathInLocationHeader()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedAbsolute");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsQualifiedPathInLocationHeader()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedQualified");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(
                "http://localhost/ActionResultsVerification/GetDummy/1",
                response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsUriInLocationHeader()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedUri");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtActionResult_GeneratesUri_WithActionAndController()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedAtAction");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtRouteResult_GeneratesUri_WithRouteValues()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedAtRoute");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtRouteResult_GeneratesUri_WithRouteName()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedAtRouteWithRouteName");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/foo/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("http://localhost/Home/Index",
            "application/json;charset=utf-8",
            "{\"test.SampleInt\":[\"" + sampleIntError + "\"]," +
            "\"test.SampleString\":" +
            "[\"" + sampleStringError + "\"]}")]
        [InlineData("http://localhost/Home/Index",
            "application/xml;charset=utf-8",
            "<Error><test.SampleInt>" + sampleIntError + "</test.SampleInt>" +
            "<test.SampleString>" + sampleStringError +
            "</test.SampleString></Error>")]
        [InlineData("http://localhost/XmlSerializer/GetSerializableError",
            "application/xml;charset=utf-8",
            "<Error><test.SampleInt>" + sampleIntError + "</test.SampleInt>" +
            "<test.SampleString>" + sampleStringError +
            "</test.SampleString></Error>")]
        public async Task SerializableErrorIsReturnedInExpectedFormat(string url, string outputFormat, string output)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/ActionResultsWebSite\">" +
                "<SampleInt>2</SampleInt><SampleString>foo</SampleString></DummyClass>";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(outputFormat));
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");
	    
            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(output, await response.Content.ReadAsStringAsync());
        }
        
        [Fact]
        public async Task SerializableError_CanSerializeNormalObjects()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/ActionResultsWebSite\">" +
                "<SampleInt>2</SampleInt><SampleString>foo</SampleString></DummyClass>";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Home/GetCustomErrorObject");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;charset=utf-8"));
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("[\"Something went wrong with the model.\"]",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SerializableError_ReadTheReturnedXml()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/ActionResultsWebSite\">" +
                "<SampleInt>20</SampleInt><SampleString>foo</SampleString></DummyClass>";

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Home/Index");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml;charset=utf-8"));
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Deserializing Xml content
            var serializer = new XmlSerializer(typeof(SerializableError));
            var errors = (SerializableError)serializer.Deserialize(
                new MemoryStream(Encoding.UTF8.GetBytes(responseContent)));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(
                "<Error><test.SampleString>" + sampleStringError + "</test.SampleString></Error>",
                responseContent);
            Assert.Equal(sampleStringError, errors["test.SampleString"]);
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsDefaultContentTypeAndEncoding()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                            HttpMethod.Post,
                            "http://localhost/ActionResultsVerification/GetContentResult");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("content", await response.Content.ReadAsStringAsync());
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsContentTypeWithDefaultEncoding()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                            HttpMethod.Post,
                            "http://localhost/ActionResultsVerification/GetContentResultWithContentType");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            Assert.Equal("content", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsContentTypeAndEncoding()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                            HttpMethod.Post,
                            "http://localhost/ActionResultsVerification/GetContentResultWithContentTypeAndEncoding");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("us-ascii", response.Content.Headers.ContentType.CharSet);
            Assert.Equal("content", await response.Content.ReadAsStringAsync());
        }
    }
}