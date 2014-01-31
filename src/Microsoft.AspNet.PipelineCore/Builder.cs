using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;

namespace Microsoft.AspNet.PipelineCore
{
    public class Builder : IBuilder
    {
        private readonly IFeatureCollection _interfaces;
        private readonly IDictionary<string, object> _properties;
        private readonly IList<Func<RequestDelegate, RequestDelegate>> _components = new List<Func<RequestDelegate, RequestDelegate>>();

        public Builder()
        {
            _interfaces = new FeatureCollection();
            _properties = new Dictionary<string, object>();
        }

        public Builder(IFeatureCollection interfaces, IDictionary<string, object> properties)
        {
            _interfaces = interfaces;
            _properties = properties;
        }

        public void Dispose()
        {
            _interfaces.Dispose();
        }

        public virtual object GetItem(Type key)
        {
            object value;
            return _interfaces.TryGetValue(key, out value);
        }

        public virtual void SetItem(Type key, object value)
        {
            _interfaces[key] = value;
        }

        public virtual object GetItem(string key)
        {
            object value;
            return _properties.TryGetValue(key, out value);
        }

        public virtual void SetItem(string key, object value)
        {
            _properties[key] = value;
        }

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
            return new Builder(_interfaces, _properties);
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
