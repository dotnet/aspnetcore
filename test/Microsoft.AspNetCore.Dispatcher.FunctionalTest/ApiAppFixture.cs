// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.Dispatcher.FunctionalTest
{
    public class ApiAppFixture : IDisposable
    {
        public ApiAppFixture()
        {
            var builder = new WebHostBuilder();
            builder.UseStartup<ApiAppStartup>();

            Server = new TestServer(builder);

            Client = Server.CreateClient();
            Client.BaseAddress = new Uri("http://locahost");
        }

        public HttpClient Client { get; }

        public TestServer Server { get; }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }
    }
}
