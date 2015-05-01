// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TryValidateModelTest
    {
        private const string SiteName = nameof(ValidationWebSite);
        private readonly Action<IApplicationBuilder> _app = new ValidationWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new ValidationWebSite.Startup().ConfigureServices;

        [Fact]
        public async Task TryValidateModel_ClearParameterValidationError_ReturnsErrorsForInvalidProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var input = "{ \"Price\": 2, \"Contact\": \"acvrdzersaererererfdsfdsfdsfsdf\", " +
                "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"}}";
            var content = new StringContent(input, Encoding.UTF8, "application/json");
            var url =
                "http://localhost/ModelMetadataTypeValidation/" +
                "TryValidateModelAfterClearingValidationErrorInParameter?theImpossibleString=test";

            // Act
            var response = await client.PostAsync(url, content);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
            Assert.Equal(4, json.Count);
            Assert.Equal("CompanyName cannot be null or empty.", json["CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", json["Price"]);
            Assert.Equal("The Category field is required.", json["Category"]);
            Assert.Equal("The field Contact Us must be a string with a maximum length of 20."+
                "The field Contact Us must match the regular expression '^[0-9]*$'.", json["Contact"]);
        }

        [Fact]
        public async Task TryValidateModel_InvalidTypeOnDerivedModel_ReturnsErrors()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url =
                "http://localhost/ModelMetadataTypeValidation/TryValidateModelSoftwareViewModelWithPrefix";

            // Act
            var response = await client.GetAsync(url);

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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var url =
                "http://localhost/ModelMetadataTypeValidation/TryValidateModelValidModelNoPrefix";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("{}", body);
        }

        [Fact]
        public async Task TryValidateModel_CollectionsModel_ReturnsErrorsForInvalidProperties()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var input = "[ { \"Price\": 2, \"Contact\": \"acvrdzersaererererfdsfdsfdsfsdf\", " +
                "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"} }," +
                "{\"Price\": 2, \"Contact\": \"acvrdzersaererererfdsfdsfdsfsdf\", " +
              "\"ProductDetails\": {\"Detail1\": \"d1\", \"Detail2\": \"d2\", \"Detail3\": \"d3\"} }]";
            var content = new StringContent(input, Encoding.UTF8, "application/json");
            var url =
                "http://localhost/ModelMetadataTypeValidation/TryValidateModelWithCollectionsModel";

            // Act
            var response = await client.PostAsync(url, content);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
            Assert.Equal("CompanyName cannot be null or empty.", json["[0].CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", json["[0].Price"]);
            Assert.Equal("The Category field is required.", json["[0].Category"]);
            Assert.Equal("The field Contact Us must be a string with a maximum length of 20." +
                "The field Contact Us must match the regular expression '^[0-9]*$'.", json["[0].Contact"]);
            Assert.Equal("CompanyName cannot be null or empty.", json["[1].CompanyName"]);
            Assert.Equal("The field Price must be between 20 and 100.", json["[1].Price"]);
            Assert.Equal("The Category field is required.", json["[1].Category"]);
            Assert.Equal("The field Contact Us must be a string with a maximum length of 20." +
                "The field Contact Us must match the regular expression '^[0-9]*$'.", json["[1].Contact"]);
        }
    }
}