using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.PipelineCore
{
    public class Builder : IBuilder
    {
        private readonly IList<Func<RequestDelegate, RequestDelegate>> _components = new List<Func<RequestDelegate, RequestDelegate>>();

        public Builder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        internal Builder(Builder builder)
        {
            ServiceProvider = builder.ServiceProvider;
            Server = builder.Server;
        }

        public IServiceProvider ServiceProvider { get; set; }
        public IServerInformation Server { get; set; }

        public IBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        public IBuilder Run(RequestDelegate handler)
        {
            return Use(next => handler);
        }

        public IBuilder New()
        {
            return new Builder(this);
        }

        public RequestDelegate Build()
        {
            RequestDelegate app = async context => context.Response.StatusCode = 404;

            foreach (var component in _components.Reverse())
            {
                app = component(app);
            }

            return app;
        }
    }
}
