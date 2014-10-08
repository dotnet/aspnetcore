// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.AspNet.Hosting
{
    public class HostingContext
    {
        public IServiceProvider Services { get; set; }
        public IConfiguration Configuration { get; set; }

        public IApplicationBuilder Builder { get; set; }

        public string ApplicationName { get; set; }
        public string EnvironmentName { get; set; }
        public Action<IApplicationBuilder> ApplicationStartup { get; set; }
        public RequestDelegate ApplicationDelegate { get; set; }

        public string ServerName { get; set; }
        public IServerFactory ServerFactory { get; set; }
        public IServerInformation Server { get; set; }
    }
}