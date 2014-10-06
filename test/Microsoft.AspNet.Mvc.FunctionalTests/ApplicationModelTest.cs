// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;
using System.Net;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ApplicationModelTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(ApplicationModelWebSite));
        private readonly Action<IApplicationBuilder> _app = new ApplicationModelWebSite.Startup().Configure;

        [Fact]
        public async Task ControllerModel_CustomizedWithAttribute()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ParameterModel/GetParameterIsOptional");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("True", body);

        }
    }
}