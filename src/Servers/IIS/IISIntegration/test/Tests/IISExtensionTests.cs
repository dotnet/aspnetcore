// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    public class IISExtensionTests
    {
        [Fact]
        public void CallingUseIISIntegrationMultipleTimesWorks()
        {

            var builder = new WebHostBuilder()
                .UseSetting("TOKEN", "TestToken")
                .UseSetting("PORT", "12345")
                .UseSetting("APPL_PATH", "/")
                .UseIISIntegration()
                .UseIISIntegration()
                .Configure(app => { });
            var server = new TestServer(builder);

            var filters = server.Host.Services.GetServices<IStartupFilter>()
                .OfType<IISSetupFilter>();

            Assert.Single(filters);
        }
    }
}
