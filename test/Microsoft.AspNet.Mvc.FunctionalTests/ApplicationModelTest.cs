// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ApplicationModelTest
    {
        private const string SiteName = nameof(ApplicationModelWebSite);
        private readonly Action<IApplicationBuilder> _app = new ApplicationModelWebSite.Startup().Configure;

        [Fact]
        public async Task ControllerModel_CustomizedWithAttribute()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/CoolController/GetControllerName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("CoolController", body);
        }

        [Fact]
        public async Task ActionModel_CustomizedWithAttribute()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ActionModel/ActionName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("ActionName", body);
        }

        [Fact]
        public async Task ParameterModel_CustomizedWithAttribute()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ParameterModel/GetParameterMetadata");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("CoolMetadata", body);
        }

        [Fact]
        public async Task ApplicationModel_AddPropertyToActionDescriptor_FromApplicationModel()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/GetCommonDescription");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Common Application Description", body);
        }

        [Fact]
        public async Task ApplicationModel_AddPropertyToActionDescriptor_ControllerModelOverwritesCommonApplicationProperty()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApplicationModel/GetControllerDescription");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Common Controller Description", body);
        }

        [Fact]
        public async Task ApplicationModel_ProvidesMetadataToActionDescriptor_ActionModelOverwritesControllerModelProperty()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApplicationModel/GetActionSpecificDescription");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Specific Action Description", body);
       }

        [Fact]
        public async Task ApplicationModelExtensions_AddsConventionToAllControllers()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Lisence/GetLisence");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Copyright (c) Microsoft Open Technologies, Inc. All rights reserved." +
                " Licensed under the Apache License, Version 2.0. See License.txt " +
                "in the project root for license information.", body);
        }

        [Fact]
        public async Task ApplicationModelExtensions_AddsConventionToAllActions()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Home/GetHelloWorld");
            request.Headers.Add("helloWorld", "HelloWorld");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("From Header - HelloWorld", body);
        }
    }
}