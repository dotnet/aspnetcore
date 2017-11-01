// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using XmlFormattersWebSite;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class XmlDataContractSerializerInputFormatterTest : IClassFixture<MvcTestFixture<Startup>>
    {
        private readonly string errorMessageFormat = string.Format(
            "{{1}}:{0} does not recognize '{1}', so instead use '{2}' with '{3}' set to '{4}' for value " +
            "type property '{{0}}' on type '{{1}}'.",
            typeof(DataContractSerializer).FullName,
            typeof(RequiredAttribute).FullName,
            typeof(DataMemberAttribute).FullName,
            nameof(DataMemberAttribute.IsRequired),
            bool.TrueString);

        public XmlDataContractSerializerInputFormatterTest(MvcTestFixture<Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

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
            Assert.Contains("An error occured while deserializing input data.", data);
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
        public async Task DataMissingForRefereneceTypeProperties_AndModelIsBound_AndHasMixedValidationErrors()
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
}