// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Infrastructure;

namespace Microsoft.AspNet.Builder
{
    public class ApplicationBuilder : IApplicationBuilder
    {
        private readonly IList<Func<RequestDelegate, RequestDelegate>> _components = new List<Func<RequestDelegate, RequestDelegate>>();

        public ApplicationBuilder(IServiceProvider serviceProvider)
        {
            Properties = new Dictionary<string, object>();
            ApplicationServices = serviceProvider;
        }

        private ApplicationBuilder(ApplicationBuilder builder)
        {
            Properties = builder.Properties;
        }

        public IServiceProvider ApplicationServices
        {
            get
            {
                return GetProperty<IServiceProvider>(Constants.BuilderProperties.ApplicationServices);
            }
            set
            {
                SetProperty<IServiceProvider>(Constants.BuilderProperties.ApplicationServices, value);
            }
        }

        public IServerInformation Server
        {
            get
            {
                return GetProperty<IServerInformation>(Constants.BuilderProperties.ServerInformation);
            }
            set
            {
                SetProperty<IServerInformation>(Constants.BuilderProperties.ServerInformation, value);
            }
        }

        public IDictionary<string, object> Properties { get; set; }

        private T GetProperty<T>(string key)
        {
            object value;
            return Properties.TryGetValue(key, out value) ? (T)value : default(T);
        }

        private void SetProperty<T>(string key, T value)
        {
            Properties[key] = value;
        }

        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        public IApplicationBuilder New()
        {
            return new ApplicationBuilder(this);
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
