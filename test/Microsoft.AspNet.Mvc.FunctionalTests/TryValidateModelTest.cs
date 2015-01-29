// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TryValidateModelTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(FormatterWebSite));
        private readonly Action<IApplicationBuilder> _app = new FormatterWebSite.Startup().Configure;

        [Fact]
        public async Task TryValidateModel_SimpleModelInvalidProperties()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/TryValidateModel/GetInvalidUser");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var json = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(
                await response.Content.ReadAsStringAsync());
            Assert.Equal("The field Id must be between 1 and 2000.", json["Id"][0]);
            Assert.Equal(
                "The field Name must be a string or array type with a minimum length of '5'.", json["Name"][0]);
        }

        [Fact]
        public async Task TryValidateModel_DerivedModelInvalidType()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/TryValidateModel/GetInvalidAdminWithPrefix");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var json = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(
                await response.Content.ReadAsStringAsync());
            Assert.Equal("AdminAccessCode property does not have the right value", json["admin"][0]);
        }

        [Fact]
        public async Task TryValidateModel_ValidDerivedModel()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/TryValidateModel/GetValidAdminWithPrefix");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Admin user created successfully", await response.Content.ReadAsStringAsync());
        }
    }
}