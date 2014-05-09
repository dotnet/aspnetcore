// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
