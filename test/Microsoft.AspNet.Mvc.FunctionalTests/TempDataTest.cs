// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TempDataTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("TempDataWebSite");
        private readonly Action<IApplicationBuilder> _app = new TempDataWebSite.Startup().Configure;

        [Fact]
        public async Task ViewRendersTempData()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.PostAsync("http://localhost/Home/DisplayTempData", content);
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);
        }
    }
}