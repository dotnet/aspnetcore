// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class XmlSerializerInputFormatterTests : IClassFixture<MvcTestFixture<XmlFormattersWebSite.Startup>>
{
    public XmlSerializerInputFormatterTests(MvcTestFixture<XmlFormattersWebSite.Startup> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task CheckIfXmlSerializerInputFormatterIsCalled()
    {
        // Arrange
        var sampleInputInt = 10;
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<DummyClass><SampleInt>"
            + sampleInputInt.ToString(CultureInfo.InvariantCulture) + "</SampleInt></DummyClass>";
        var content = new StringContent(input, Encoding.UTF8, "application/xml-xmlser");

        // Act
        var response = await Client.PostAsync("http://localhost/Home/Index", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(sampleInputInt.ToString(CultureInfo.InvariantCulture), await response.Content.ReadAsStringAsync());
    }

    [ConditionalFact]
    // Mono.Xml2.XmlTextReader.ReadText is unable to read the XML. This is fixed in mono 4.3.0.
    [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
    public async Task ThrowsOnInvalidInput_AndAddsToModelState()
    {
        // Arrange
        var input = "Not a valid xml document";
        var content = new StringContent(input, Encoding.UTF8, "application/xml-xmlser");

        // Act
        var response = await Client.PostAsync("http://localhost/Home/Index", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadAsStringAsync();
        Assert.Contains("An error occurred while deserializing input data.", data);
    }
}
