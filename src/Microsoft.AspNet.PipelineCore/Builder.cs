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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder
{
    public class Builder : IBuilder
    {
        private readonly IList<Func<RequestDelegate, RequestDelegate>> _components = new List<Func<RequestDelegate, RequestDelegate>>();

        public Builder(IServiceProvider serviceProvider)
        {
            ApplicationServices = serviceProvider;
        }

        private Builder(Builder builder)
        {
            ApplicationServices = builder.ApplicationServices;
            Server = builder.Server;
        }

        public IServiceProvider ApplicationServices { get; set; }
        public IServerInformation Server { get; set; }

        public IBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        public IBuilder New()
        {
            return new Builder(this);
        }

        public RequestDelegate Build()
        {
            RequestDelegate app = context =>
            {
                context.Response.StatusCode = 404;
                return Task.FromResult(0);
            };

            foreach (var component in _components.Reverse())
            {
                app = component(app);
            }

            return app;
        }
    }
}
