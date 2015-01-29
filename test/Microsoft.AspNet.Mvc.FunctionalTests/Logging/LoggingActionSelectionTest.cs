// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50  // Since Json.net serialization fails in CoreCLR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LoggingWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class LoggingActionSelectionTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(LoggingWebSite));
        private readonly Action<IApplicationBuilder> _app = new LoggingWebSite.Startup().Configure;
        
        [Fact]
        public async Task Successful_ActionSelection_Logged()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var requestTraceId = Guid.NewGuid().ToString();

            // Act
            var response = await client.GetAsync(string.Format(
                                                        "http://localhost/home/index?{0}={1}",
                                                        LoggingExtensions.RequestTraceIdQueryKey, 
                                                        requestTraceId));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal("Home.Index", responseData);

            var logs = await GetLogsAsync(client, requestTraceId);
            var scopeNode = logs.FindScope(nameof(MvcRouteHandler) + ".RouteAsync");

            Assert.NotNull(scopeNode);
            var logInfo = scopeNode.Messages.OfDataType<MvcRouteHandlerRouteAsyncValues>()
                                            .FirstOrDefault();
                        
            Assert.NotNull(logInfo);
            Assert.NotNull(logInfo.State);

            dynamic actionSelection = logInfo.State;
            Assert.True((bool)actionSelection.ActionSelected);
            Assert.True((bool)actionSelection.ActionInvoked);
            Assert.True((bool)actionSelection.Handled);
        }

        [Fact]
        public async Task Failed_ActionSelection_Logged()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var requestTraceId = Guid.NewGuid().ToString();

            // Act
            var response = await client.GetAsync(string.Format(
                                                        "http://localhost/InvalidController/InvalidAction?{0}={1}",
                                                        LoggingExtensions.RequestTraceIdQueryKey, 
                                                        requestTraceId));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var logs = await GetLogsAsync(client, requestTraceId);
            var scopeNode = logs.FindScope(nameof(MvcRouteHandler) + ".RouteAsync");

            Assert.NotNull(scopeNode);
            var logInfo = scopeNode.Messages.OfDataType<MvcRouteHandlerRouteAsyncValues>()
                                            .FirstOrDefault();
            Assert.NotNull(logInfo);

            dynamic actionSelection = logInfo.State;
            Assert.False((bool)actionSelection.ActionSelected);
            Assert.False((bool)actionSelection.ActionInvoked);
            Assert.False((bool)actionSelection.Handled);
        }

        private async Task<IEnumerable<ActivityContextDto>> GetLogsAsync(HttpClient client,
                                                                    string requestTraceId)
        {
            var responseData = await client.GetStringAsync("http://localhost/logs");
            var logActivities = JsonConvert.DeserializeObject<List<ActivityContextDto>>(responseData);
            return logActivities.FilterByRequestTraceId(requestTraceId);
        }

    }
}
#endif