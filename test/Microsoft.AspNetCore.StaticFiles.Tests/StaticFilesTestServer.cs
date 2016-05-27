// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.StaticFiles
{
    public static class StaticFilesTestServer
    {
        public static TestServer Create(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices = null)
        {
            var contentRootNet451 = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                "." : "../../../../test/Microsoft.AspNetCore.StaticFiles.Tests";
            Action<IServiceCollection> defaultConfigureServices = services => { };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new []
                {
                    new KeyValuePair<string, string>("webroot", ".")
                })
                .Build();
            var builder = new WebHostBuilder()
#if NET451
                .UseContentRoot(contentRootNet451)
#endif
                .UseConfiguration(configuration)
                .Configure(configureApp)
                .ConfigureServices(configureServices ?? defaultConfigureServices);
            return new TestServer(builder);
        }
    }
}