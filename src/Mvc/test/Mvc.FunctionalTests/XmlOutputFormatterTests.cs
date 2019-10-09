// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class XmlOutputFormatterTests : IClassFixture<MvcTestFixture<FormatterWebSite.Startup>>
    {
        public XmlOutputFormatterTests(MvcTestFixture<FormatterWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [ConditionalFact]
        // Mono.Xml2.XmlTextReader.ReadText is unable to read the XML. This is fixed in mono 4.3.0.
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task XmlDataContractSerializerOutputFormatterIsCalled()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/Home/GetDummyClass?sampleInput=10");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<SampleInt>10</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
            Assert.Equal(167, response.Content.Headers.ContentLength);
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterIsCalled()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/XmlSerializer/GetDummyClass?sampleInput=10");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
            Assert.Equal(149, response.Content.Headers.ContentLength);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task XmlSerializerFailsAndDataContractSerializerIsCalled()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/DataContractSerializer/GetPerson?name=HelloWorld");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<Person xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<Name>HelloWorld</Name></Person>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task XmlSerializerOutputFormatter_WhenDerivedClassIsReturned()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/XmlSerializer/GetDerivedDummyClass?sampleInput=10");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:type=\"DerivedDummyClass\">" +
                "<SampleInt>10</SampleInt><SampleIntInDerived>50</SampleIntInDerived></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [ConditionalFact]
        // Mono.Xml2.XmlTextReader.ReadText is unable to read the XML. This is fixed in mono 4.3.0.
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task XmlDataContractSerializerOutputFormatter_WhenDerivedClassIsReturned()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/Home/GetDerivedDummyClass?sampleInput=10");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "i:type=\"DerivedDummyClass\" xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\"" +
                "><SampleInt>10</SampleInt><SampleIntInDerived>50</SampleIntInDerived></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task XmlSerializerFormatter_DoesNotWriteDictionaryObjects()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/XmlSerializer/GetDictionary");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        // Xml-based formatters are sensitive to ObjectResult.DeclaredType of the result. A couple of
        // tests to verify we don't regress these.
        [Fact]
        public async Task XmlSerializerFormatter_WorksForActionsReturningTaskOfDummyClass()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/XmlSerializer/GetTaskOfDummyClass");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task XmlSerializerFormatter_WorksForActionsReturningDummyClassAsTaskOfObject()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/XmlSerializer/GetTaskOfDummyClassAsObject");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task XmlSerializerOutputFormatter_WorksForActionsReturningTaskOfPerson()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/DataContractSerializer/GetTaskOfPerson?name=HelloWorld");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<Person xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<Name>HelloWorld</Name></Person>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task XmlSerializerOutputFormatter_WorksForActionsReturningPersonAsTaskOfObject()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/DataContractSerializer/GetTaskOfPersonAsObject?name=HelloWorld");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            XmlAssert.Equal(
                "<Person xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<Name>HelloWorld</Name></Person>",
                await response.Content.ReadAsStringAsync());
        }
    }
}
