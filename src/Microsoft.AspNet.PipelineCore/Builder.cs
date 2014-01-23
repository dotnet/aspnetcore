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
        private readonly IInterfaceDictionary _interfaces;
        private readonly IDictionary<string, object> _properties;
        private readonly IList<Entry> _components = new List<Entry>();

        public Builder()
        {
            _interfaces = new InterfaceDictionary();
            _properties = new Dictionary<string, object>();
        }

        public Builder(IInterfaceDictionary interfaces, IDictionary<string, object> properties)
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

        public IBuilder Use(object middleware, params object[] args)
        {
            _components.Add(new Entry(middleware, args));
            return this;
        }

        class Entry
        {
            private readonly object _middleware;
            private readonly object[] _args;

            public Entry(object middleware, object[] args)
            {
                _middleware = middleware;
                _args = args;
            }
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
                app = Activate(component, app);
            }

            return app;
        }

        private RequestDelegate Activate(Entry component, RequestDelegate app)
        {
            return app;
        }

        public Func<TContext, Task> Adapt<TContext>(object app)
        {
            return null;
        }
    }
}
