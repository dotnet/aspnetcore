// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Mvc.Testing;
using XmlFormattersWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class XmlSerializerFormattersWrappingTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Theory]
    [InlineData("http://localhost/IEnumerable/ValueTypes")]
    [InlineData("http://localhost/IQueryable/ValueTypes")]
    public async Task CanWrite_ValueTypes(string url)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml-xmlser"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal("<ArrayOfInt xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                     "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><int>10</int>" +
                     "<int>20</int></ArrayOfInt>",
                     result);
    }

    [Theory]
    [InlineData("http://localhost/IEnumerable/NonWrappedTypes")]
    [InlineData("http://localhost/IQueryable/NonWrappedTypes")]
    public async Task CanWrite_NonWrappedTypes(string url)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml-xmlser"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal("<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                     "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><string>value1</string>" +
                     "<string>value2</string></ArrayOfString>",
                     result);
    }

    [Theory]
    [InlineData("http://localhost/IEnumerable/NonWrappedTypes_NullInstance")]
    [InlineData("http://localhost/IQueryable/NonWrappedTypes_NullInstance")]
    public async Task CanWrite_NonWrappedTypes_NullInstance(string url)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml-xmlser"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal("<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
            " xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:nil=\"true\" />",
            result);
    }

    [Theory]
    [InlineData("http://localhost/IEnumerable/NonWrappedTypes_Empty")]
    [InlineData("http://localhost/IQueryable/NonWrappedTypes_Empty")]
    public async Task CanWrite_NonWrappedTypes_Empty(string url)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml-xmlser"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal("<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
            " xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />",
            result);
    }

    [Theory]
    [InlineData("http://localhost/IEnumerable/WrappedTypes")]
    [InlineData("http://localhost/IQueryable/WrappedTypes")]
    public async Task CanWrite_WrappedTypes(string url)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml-xmlser"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal("<ArrayOfPersonWrapper xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                     "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><PersonWrapper><Id>10</Id>" +
                     "<Name>Mike</Name><Age>35</Age></PersonWrapper><PersonWrapper><Id>11</Id>" +
                     "<Name>Jimmy</Name><Age>35</Age></PersonWrapper></ArrayOfPersonWrapper>",
                     result);
    }

    [Theory]
    [InlineData("http://localhost/IEnumerable/WrappedTypes_Empty")]
    [InlineData("http://localhost/IQueryable/WrappedTypes_Empty")]
    public async Task CanWrite_WrappedTypes_Empty(string url)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml-xmlser"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal("<ArrayOfPersonWrapper xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
            " xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />",
            result);
    }

    [Theory]
    [InlineData("http://localhost/IEnumerable/WrappedTypes_NullInstance")]
    [InlineData("http://localhost/IQueryable/WrappedTypes_NullInstance")]
    public async Task CanWrite_WrappedTypes_NullInstance(string url)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml-xmlser"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal("<ArrayOfPersonWrapper xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
            " xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:nil=\"true\" />",
            result);
    }

    [Fact]
    public async Task CanWrite_IEnumerableOf_SerializableErrors()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/IEnumerable/SerializableErrors");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml-xmlser"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal("<ArrayOfSerializableErrorWrapper xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
            " xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SerializableErrorWrapper><key1>key1-error</key1>" +
            "<key2>key2-error</key2></SerializableErrorWrapper><SerializableErrorWrapper><key3>key1-error</key3>" +
            "<key4>key2-error</key4></SerializableErrorWrapper></ArrayOfSerializableErrorWrapper>",
            result);
    }

    [Fact]
    public async Task ProblemDetails_IsSerialized()
    {
        // Arrange
        using (new ActivityReplacer())
        {
            var expected = "<problem xmlns=\"urn:ietf:rfc:7807\">" +
                "<status>404</status>" +
                "<title>Not Found</title>" +
                "<type>https://tools.ietf.org/html/rfc9110#section-15.5.5</type>" +
                $"<traceId>{Activity.Current.Id}</traceId>" +
                "</problem>";

            // Act
            var response = await Client.GetAsync("/api/XmlSerializerApi/ActionReturningClientErrorStatusCodeResult");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            var root = XDocument.Parse(content).Root;
            Assert.Equal("404", root.Element(root.Name.Namespace.GetName("status"))?.Value);
            Assert.Equal("Not Found", root.Element(root.Name.Namespace.GetName("title"))?.Value);
            Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", root.Element(root.Name.Namespace.GetName("type"))?.Value);
            // Activity is not null
            Assert.NotNull(root.Element(root.Name.Namespace.GetName("traceId"))?.Value);
        }
    }

    [Fact]
    public async Task ProblemDetails_WithExtensionMembers_IsSerialized()
    {
        // Arrange
        var expected = "<problem xmlns=\"urn:ietf:rfc:7807\">" +
            "<instance>instance</instance>" +
            "<status>404</status>" +
            "<title>title</title>" +
            "<Correlation>correlation</Correlation>" +
            "<Accounts>Account1 Account2</Accounts>" +
            "</problem>";

        // Act
        var response = await Client.GetAsync("/api/XmlSerializerApi/ActionReturningProblemDetails");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal(expected, content);
    }

    [Fact]
    public async Task ValidationProblemDetails_IsSerialized()
    {
        // Arrange
        using (new ActivityReplacer())
        {
            // Act
            var response = await Client.GetAsync("/api/XmlSerializerApi/ActionReturningValidationProblem");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            var root = XDocument.Parse(content).Root;
            Assert.Equal("400", root.Element(root.Name.Namespace.GetName("status"))?.Value);
            Assert.Equal("One or more validation errors occurred.", root.Element(root.Name.Namespace.GetName("title"))?.Value);
            var mvcErrors = root.Element(root.Name.Namespace.GetName("MVC-Errors"));
            Assert.NotNull(mvcErrors);
            Assert.Equal("The State field is required.", mvcErrors.Element(root.Name.Namespace.GetName("State"))?.Value);
            // Activity is not null
            Assert.NotNull(root.Element(root.Name.Namespace.GetName("traceId"))?.Value);
        }
    }

    [Fact]
    public async Task ValidationProblemDetails_WithExtensionMembers_IsSerialized()
    {
        // Arrange
        var expected = "<problem xmlns=\"urn:ietf:rfc:7807\">" +
            "<detail>some detail</detail>" +
            "<status>400</status>" +
            "<title>One or more validation errors occurred.</title>" +
            "<type>some type</type>" +
            "<CorrelationId>correlation</CorrelationId>" +
            "<MVC-Errors>" +
            "<Error1>ErrorValue</Error1>" +
            "</MVC-Errors>" +
            "</problem>";

        // Act
        var response = await Client.GetAsync("/api/XmlSerializerApi/ActionReturningValidationDetailsWithMetadata");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal(expected, content);
    }
}
