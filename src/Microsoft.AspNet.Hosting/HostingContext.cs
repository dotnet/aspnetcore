// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public class HostingContext
    {
        public IConfiguration Configuration { get; set; }

        public IApplicationBuilder Builder { get; set; }

        public string ApplicationName { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; }
        public StartupMethods StartupMethods { get; set; }
        public RequestDelegate ApplicationDelegate { get; set; }

        public IServiceCollection Services { get; } = new ServiceCollection();

        // Result of ConfigureServices
        public IServiceProvider ApplicationServices { get; set; }

        public string ServerFactoryLocation { get; set; }
        public IServerFactory ServerFactory { get; set; }
        public IServerInformation Server { get; set; }
    }
}