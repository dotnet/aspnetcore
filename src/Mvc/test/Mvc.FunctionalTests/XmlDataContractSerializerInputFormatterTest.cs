// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;
using XmlFormattersWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class XmlDataContractSerializerInputFormatterTest : LoggedTest
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

    public HttpClient Client { get; private set; }

    public MvcTestFixture<Startup> Factory { get; private set; }

    [Fact]
    public async Task ThrowsOnInvalidInput_AndAddsToModelState()
    {
        // Arrange
        var input = "Not a valid xml document";
        var content = new StringContent(input, Encoding.UTF8, "application/xml-dcs");

        // Act
        var response = await Client.PostAsync("http://localhost/Home/Index", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadAsStringAsync();
        Assert.Contains("An error occurred while deserializing input data.", data);
    }

    [Fact]
    public async Task RequiredDataIsProvided_AndModelIsBound_NoValidationErrors()
    {
        // Arrange
        var input = "<Store xmlns=\"http://schemas.datacontract.org/2004/07/XmlFormattersWebSite\" " +
            "xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Address><State>WA</State><Zipcode>" +
            "98052</Zipcode></Address><Id>10</Id></Store>";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Validation/CreateStore");
        request.Content = new StringContent(input, Encoding.UTF8, "application/xml-dcs");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml-dcs"));

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var dcsSerializer = new DataContractSerializer(typeof(ModelBindingInfo));
        var responseStream = await response.Content.ReadAsStreamAsync();
        var modelBindingInfo = dcsSerializer.ReadObject(responseStream) as ModelBindingInfo;
        Assert.NotNull(modelBindingInfo);
        Assert.NotNull(modelBindingInfo.Store);
        Assert.Equal(10, modelBindingInfo.Store.Id);
        Assert.NotNull(modelBindingInfo.Store.Address);
        Assert.Equal(98052, modelBindingInfo.Store.Address.Zipcode);
        Assert.Equal("WA", modelBindingInfo.Store.Address.State);
        Assert.Empty(modelBindingInfo.ModelStateErrorMessages);
    }

    // Verifies that the model state has errors related to body model validation.
    [Fact]
    public async Task DataMissingForReferenceTypeProperties_AndModelIsBound_AndHasMixedValidationErrors()
    {
        // Arrange
        var input = "<Store xmlns=\"http://schemas.datacontract.org/2004/07/XmlFormattersWebSite\"" +
            " xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
            "<Address i:nil=\"true\"/><Id>10</Id></Store>";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Validation/CreateStore");
        request.Content = new StringContent(input, Encoding.UTF8, "application/xml-dcs");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml-dcs"));

        var expectedErrorMessages = new List<string>();
        expectedErrorMessages.Add("Address:The Address field is required.");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        var dcsSerializer = new DataContractSerializer(typeof(ModelBindingInfo));
        var responseStream = await response.Content.ReadAsStreamAsync();
        var modelBindingInfo = dcsSerializer.ReadObject(responseStream) as ModelBindingInfo;
        Assert.NotNull(modelBindingInfo);
        Assert.NotNull(modelBindingInfo.Store);
        Assert.Equal(10, modelBindingInfo.Store.Id);
        Assert.NotNull(modelBindingInfo.ModelStateErrorMessages);

        Assert.Equal(expectedErrorMessages.Count(), modelBindingInfo.ModelStateErrorMessages.Count);
        foreach (var expectedErrorMessage in expectedErrorMessages)
        {
            Assert.Contains(
                modelBindingInfo.ModelStateErrorMessages,
                (actualErrorMessage) => actualErrorMessage.Equals(expectedErrorMessage));
        }
    }
}
