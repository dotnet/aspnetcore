using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpEnvironment;
using Microsoft.AspNet.Interfaces;
using Microsoft.AspNet.PipelineCore.Owin;

namespace Microsoft.AspNet.PipelineCore
{
    public class Builder : IBuilder
    {
        private readonly IFeatureContainer _features;
        private readonly IList<Entry> _components = new List<Entry>();

        public Builder()
        {
            _features = new FeatureModel.FeatureContainer();
        }

        public Builder(IFeatureContainer features)
        {
            _features = features;
        }

        public void Dispose()
        {
            _features.Dispose();
        }

        public virtual object GetFeature(Type type)
        {
            return _features.GetFeature(type);
        }

        public virtual void SetFeature(Type type, object feature)
        {
            _features.SetFeature(type, feature);
        }

        public virtual int Revision
        {
            get { return _features.Revision; }
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
            return new Builder(_features);
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
