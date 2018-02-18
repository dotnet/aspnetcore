// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class ServerFactory : IDisposable
    {
        private bool disposedValue = false;
        private IList<IDisposable> _disposableServers = new List<IDisposable>();

        public TestServer CreateServer(
            Action<IWebHostBuilder> configureBuilder, 
            [CallerMemberName] string isolationKey = "")
        {
            var builder = WebHostBuilderFactory
                .CreateFromTypesAssemblyEntryPoint<Startup>(new string[] { })
                .UseSolutionRelativeContentRoot(Path.Combine("test", "WebSites", "Identity.DefaultUI.WebSite"))
                .ConfigureServices(sc => sc.SetupTestDatabase(isolationKey)
                    .AddMvc()
                    // Mark the cookie as essential for right now, as Identity uses it on
                    // several places to pass important data in post-redirect-get flows.
                    .AddCookieTempDataProvider(o => o.Cookie.IsEssential = true));

            configureBuilder(builder);

            var server = new TestServer(builder);
            _disposableServers.Add(server);
            return server;
        }

        public TestServer CreateDefaultServer([CallerMemberName] string isolationKey = "") =>
            CreateServer(b => { }, isolationKey);

        public HttpClient CreateDefaultClient(TestServer server)
        {
            var client = new HttpClient(new CookieContainerHandler(server.CreateHandler()))
            {
                BaseAddress = new Uri("https://localhost")
            };

            return client;
        }

        public HttpClient CreateDefaultClient() =>
            CreateDefaultClient(CreateDefaultServer());

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var disposable in _disposableServers)
                    {
                        disposable?.Dispose();
                    }
                }

                _disposableServers = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
