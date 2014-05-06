// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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

        public IBuilder Builder { get; set; }

        public string ApplicationName { get; set; }
        public Action<IBuilder> ApplicationStartup { get; set; }
        public RequestDelegate ApplicationDelegate { get; set; }

        public string ServerName { get; set; }
        public IServerFactory ServerFactory { get; set; }
        public IServerInformation Server { get; set; }
    }
}