// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TryValidateModelTest : IClassFixture<MvcTestFixture<ValidationWebSite.Startup>>
    {
        public TryValidateModelTest(MvcTestFixture<ValidationWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task TryValidateModel_ClearParameterValidationError_ReturnsErrorsForInvalidProperties()
        {
            // Arrange
            var input = "{ \"Price\": 2, \"Contact\": \"acvrdzersaererererfdsfdsfdsfsdf\", " +
                "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"}}";
            var content = new StringContent(input, Encoding.UTF8, "application/json");
            var url =
                "http://localhost/ModelMetadataTypeValidation/" +
                "TryValidateModelAfterClearingValidationErrorInParameter?theImpossibleString=test";

            // Act
            var response = await Client.PostAsync(url, content);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
            Assert.Equal(4, json.Count);
            Assert.Equal("CompanyName cannot be null or empty.", json["CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", json["Price"]);
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(
                PlatformNormalizer.NormalizeContent("The Category field is required."),
                json["Category"]);
            AssertErrorEquals(
                "The field Contact Us must be a string with a maximum length of 20." +
                "The field Contact Us must match the regular expression " +
                (TestPlatformHelper.IsMono ? "^[0-9]*$." : "'^[0-9]*$'."),
                json["Contact"]);
        }

        [Fact]
        public async Task TryValidateModel_InvalidTypeOnDerivedModel_ReturnsErrors()
        {
            // Arrange
            var url = "http://localhost/ModelMetadataTypeValidation/TryValidateModelSoftwareViewModelWithPrefix";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
            Assert.Equal(1, json.Count);
            Assert.Equal("Product must be made in the USA if it is not named.", json["software"]);
        }

        [Fact]
        public async Task TryValidateModel_ValidDerivedModel_ReturnsEmptyResponseBody()
        {
            // Arrange
            var url = "http://localhost/ModelMetadataTypeValidation/TryValidateModelValidModelNoPrefix";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("{}", body);
        }

        [Fact]
        public async Task TryValidateModel_CollectionsModel_ReturnsErrorsForInvalidProperties()
        {
            // Arrange
            var input = "[ { \"Price\": 2, \"Contact\": \"acvrdzersaererererfdsfdsfdsfsdf\", " +
                "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"} }," +
                "{\"Price\": 2, \"Contact\": \"acvrdzersaererererfdsfdsfdsfsdf\", " +
              "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"} }]";
            var content = new StringContent(input, Encoding.UTF8, "application/json");
            var url =
                "http://localhost/ModelMetadataTypeValidation/TryValidateModelWithCollectionsModel";

            // Act
            var response = await Client.PostAsync(url, content);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
            Assert.Equal("CompanyName cannot be null or empty.", json["[0].CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", json["[0].Price"]);
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(
                PlatformNormalizer.NormalizeContent("The Category field is required."),
                json["[0].Category"]);
            AssertErrorEquals(
                "The field Contact Us must be a string with a maximum length of 20." +
                "The field Contact Us must match the regular expression " +
                (TestPlatformHelper.IsMono ? "^[0-9]*$." : "'^[0-9]*$'."),
                json["[0].Contact"]);
            Assert.Equal("CompanyName cannot be null or empty.", json["[1].CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", json["[1].Price"]);
            Assert.Equal(
                PlatformNormalizer.NormalizeContent("The Category field is required."),
                json["[1].Category"]);
            AssertErrorEquals(
                "The field Contact Us must be a string with a maximum length of 20." +
                "The field Contact Us must match the regular expression " +
                (TestPlatformHelper.IsMono ? "^[0-9]*$." : "'^[0-9]*$'."),
                json["[1].Contact"]);
        }

        private void AssertErrorEquals(string expected, string actual)
        {
            // OrderBy is used because the order of the results may very depending on the platform / client.
            Assert.Equal(
                expected.Split('.').OrderBy(item => item, StringComparer.Ordinal),
                actual.Split('.').OrderBy(item => item, StringComparer.Ordinal));
        }
    }
}