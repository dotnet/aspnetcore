// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class XmlSerializerInputFormatterTests : LoggedTest
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
