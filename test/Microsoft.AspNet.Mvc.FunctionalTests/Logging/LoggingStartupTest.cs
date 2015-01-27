// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50  // Since Json.net serialization fails in CoreCLR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoggingWebSite;
using LoggingWebSite.Controllers;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class LoggingStartupTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(LoggingWebSite));
        private readonly Action<IApplicationBuilder> _app = new LoggingWebSite.Startup().Configure;

        [Fact]
        public async Task AssemblyValues_LoggedAtStartup()
        {
            // Arrange and Act
            var logs = await GetLogsByDataTypeAsync<AssemblyValues>();

            // Assert
            Assert.NotEmpty(logs);
            foreach (var log in logs)
            {
                dynamic assembly = log.State;
                Assert.NotNull(assembly);
                Assert.Equal(
                    "LoggingWebSite, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                    assembly.AssemblyName.ToString());
            }
        }

        [Fact]
        public async Task ControllerModelValues_LoggedAtStartup()
        {
            // Arrange and Act
            var logs = await GetLogsByDataTypeAsync<ControllerModelValues>();

            // Assert
            Assert.Single(logs);
            dynamic controller = logs.First().State;
            Assert.Equal("Home", controller.ControllerName.ToString());
            Assert.Equal(typeof(HomeController).AssemblyQualifiedName, controller.ControllerType.ToString());
            Assert.Equal("Index", controller.Actions[0].ActionName.ToString());
            Assert.Empty(controller.ApiExplorer.IsVisible);
            Assert.Empty(controller.ApiExplorer.GroupName.ToString());
            Assert.Empty(controller.Attributes);
            Assert.Empty(controller.ActionConstraints);
            Assert.Empty(controller.RouteConstraints);
            Assert.Empty(controller.AttributeRoutes);

            var filter = Assert.Single(controller.Filters);
            Assert.Equal(typeof(ControllerActionFilter).AssemblyQualifiedName, (string)filter.FilterMetadataType);
        }

        [Fact]
        public async Task ActionDescriptorValues_LoggedAtStartup()
        {
            // Arrange and Act
            var logs = await GetLogsByDataTypeAsync<ActionDescriptorValues>();

            // Assert
            Assert.Single(logs);
            dynamic action = logs.First().State;
            Assert.Equal("Index", action.Name.ToString());
            Assert.Empty(action.Parameters);
            Assert.Equal("action", action.RouteConstraints[0].RouteKey.ToString());
            Assert.Equal("controller", action.RouteConstraints[1].RouteKey.ToString());
            Assert.Empty(action.RouteValueDefaults);
            Assert.Empty(action.ActionConstraints.ToString());
            Assert.Empty(action.HttpMethods.ToString());
            Assert.Empty(action.Properties);
            Assert.Equal("Home", action.ControllerName.ToString());

            var filter = Assert.Single(action.FilterDescriptors).Filter;
            Assert.Equal(typeof(ControllerActionFilter).AssemblyQualifiedName, (string)filter.FilterMetadataType);
        }

        private async Task<IEnumerable<LogInfoDto>> GetLogsByDataTypeAsync<T>()
        {
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var response = await client.GetStringAsync("http://localhost/logs");

            var activityDtos = JsonConvert.DeserializeObject<List<ActivityContextDto>>(response);

            var logs = activityDtos.FilterByStartup().GetLogsByDataType<T>();

            return logs;
        }
    }
}
#endif