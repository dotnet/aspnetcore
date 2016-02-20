// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests
{
    public class TestFixture<TStartup> : IDisposable
    {
        private readonly TestServer _server;

        public TestFixture()
        {
            // RequestLocalizationOptions saves the current culture when constructed, potentially changing response
            // localization i.e. RequestLocalizationMiddleware behavior. Ensure the saved culture
            // (DefaultRequestCulture) is consistent regardless of system configuration or personal preferences.
            using (new CultureReplacer())
            {
                var builder = new WebHostBuilder()
                    .UseStartup(typeof(TStartup));

                _server = new TestServer(builder);
            }

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
