// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Antiforgery.FunctionalTests
{
    public class AntiForgerySampleTestFixture : IDisposable
    {
        private readonly TestServer _server;

        public AntiForgerySampleTestFixture()
        {
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("webroot", "wwwroot")
            });

            var builder = new WebHostBuilder()
                .UseConfiguration(configurationBuilder.Build())
                .UseStartup(typeof(AntiforgerySample.Startup));

            _server = new TestServer(builder);

            Client = _server.CreateClient();
            Client.BaseAddress = new Uri("http://localhost");
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }
}
