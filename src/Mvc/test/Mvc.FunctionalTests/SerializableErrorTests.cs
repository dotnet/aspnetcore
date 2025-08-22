// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SerializableErrorTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<XmlFormattersWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<XmlFormattersWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    public static TheoryData<string> AcceptHeadersData
    {
        get
        {
            return new TheoryData<string>
                {
                    "application/xml-dcs",
                    "application/xml-xmlser"
                };
        }
    }

    [Theory]
    [MemberData(nameof(AcceptHeadersData))]
    public async Task ModelStateErrors_AreSerialized(string acceptHeader)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/SerializableError/ModelStateErrors");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
        var expectedXml = "<Error><key1>key1-error</key1><key2>The input was not valid.</key2></Error>";

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal(acceptHeader, response.Content.Headers.ContentType.MediaType);
        var responseData = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal(expectedXml, responseData);
    }

    [Theory]
    [MemberData(nameof(AcceptHeadersData))]
    public async Task PostedSerializableError_IsBound(string acceptHeader)
    {
        // Arrange
        var expectedXml = "<Error><key1>key1-error</key1><key2>The input was not valid.</key2></Error>";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/SerializableError/LogErrors")
        {
            Content = new StringContent(expectedXml, Encoding.UTF8, acceptHeader)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal(acceptHeader, response.Content.Headers.ContentType.MediaType);
        var responseData = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal(expectedXml, responseData);
    }

    public static TheoryData<string, string> InvalidInputAndHeadersData
    {
        get
        {
            return new TheoryData<string, string>
                {
                    {
                        "application/xml-dcs",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<Employee xmlns =\"http://schemas.datacontract.org/2004/07/XmlFormattersWebSite.Models\">" +
                        "<Id>2</Id><Name>foo</Name></Employee>"
                    },
                    {
                        "application/xml-xmlser",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<Employee>" +
                        "<Id>2</Id><Name>foo</Name></Employee>"
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidInputAndHeadersData))]
    public async Task IsReturnedInExpectedFormat(string acceptHeader, string inputXml)
    {
        // Arrange
        var expected = "<Error><Id>The field Id must be between 10 and 100.</Id>" +
            "<Name>The field Name must be a string or array type with a minimum " +
            "length of '15'.</Name></Error>";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/SerializableError/CreateEmployee");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
        request.Content = new StringContent(inputXml, Encoding.UTF8, acceptHeader);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseData = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal(expected, responseData);
    }

    public static TheoryData<string, string> IncorrectTopLevelInputAndHeadersData
    {
        get
        {
            return new TheoryData<string, string>
                {
                    {
                        "application/xml-dcs",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<Employees xmlns =\"http://schemas.datacontract.org/2004/07/XmlFormattersWebSite.Models\">" +
                        "<Id>2</Id><Name>foo</Name></Employee>"
                    },
                    {
                        "application/xml-xmlser",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                        "<Employees>" +
                        "<Id>2</Id><Name>foo</Name></Employee>"
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(IncorrectTopLevelInputAndHeadersData))]
    public async Task IncorrectTopLevelElement_ReturnsExpectedError(string acceptHeader, string inputXml)
    {
        // Arrange
        var expected = "<Error><MVC-Empty>An error occurred while deserializing input data.</MVC-Empty></Error>";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/SerializableError/CreateEmployee");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
        request.Content = new StringContent(inputXml, Encoding.UTF8, acceptHeader);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var responseData = await response.Content.ReadAsStringAsync();
        XmlAssert.Equal(expected, responseData);
    }
}
