// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.StaticFiles
{
    public static class StaticFilesTestServer
    {
        public static TestServer Create(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices = null)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new []
            {
                new KeyValuePair<string, string>("webroot", ".")
            });
            var builder = new WebApplicationBuilder()
                .UseConfiguration(configurationBuilder.Build())
                .Configure(configureApp)
                .ConfigureServices(configureServices);
            return new TestServer(builder);
        }
    }
}