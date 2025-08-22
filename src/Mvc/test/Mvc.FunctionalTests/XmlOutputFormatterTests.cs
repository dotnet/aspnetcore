// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class XmlOutputFormatterTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<FormatterWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public HttpClient Client { get; private set; }

    public MvcTestFixture<FormatterWebSite.Startup> Factory { get; private set; }

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
