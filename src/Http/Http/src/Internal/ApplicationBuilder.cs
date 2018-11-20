// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Builder.Internal
{
    public class ApplicationBuilder : IApplicationBuilder
    {
        private readonly IList<Func<RequestDelegate, RequestDelegate>> _components = new List<Func<RequestDelegate, RequestDelegate>>();

        public ApplicationBuilder(IServiceProvider serviceProvider)
        {
            Properties = new Dictionary<string, object>(StringComparer.Ordinal);
            ApplicationServices = serviceProvider;
        }

        public ApplicationBuilder(IServiceProvider serviceProvider, object server)
            : this(serviceProvider)
        {
            SetProperty(Constants.BuilderProperties.ServerFeatures, server);
        }

        private ApplicationBuilder(ApplicationBuilder builder)
        {
            Properties = new CopyOnWriteDictionary<string, object>(builder.Properties, StringComparer.Ordinal);
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

        public IFeatureCollection ServerFeatures
        {
            get
            {
                return GetProperty<IFeatureCollection>(Constants.BuilderProperties.ServerFeatures);
            }
        }

        public IDictionary<string, object> Properties { get; }

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
                return Task.CompletedTask;
            };

            foreach (var component in _components.Reverse())
            {
                app = component(app);
            }

            return app;
        }
    }
}
